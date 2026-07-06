using ERS_Domain;
using ERS_Domain.clsUtilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ws_GetResult_RemoteSigning
{
    // =====================================================================
    // 1) CẤU HÌNH QUEUE CHUẨN: Main - Retry - DLQ
    // =====================================================================
    /// <summary>
    /// Gom mọi thông số của 1 "cụm queue" (Main / Retry / DLQ) vào 1 chỗ,
    /// thay vì hard-code tên queue rải rác như code gốc.
    /// </summary>
    public class RabbitQueueOptions
    {
        /// <summary>Exchange dùng để publish. Để "" nếu dùng default exchange.</summary>
        public string ExchangeName { get; set; } = "";

        public string MainQueue { get; set; }
        public string RetryQueue { get; set; }
        public string DeadLetterQueue { get; set; }

        /// <summary>Thời gian chờ (ms) trước khi 1 message trong Retry Queue được đẩy lại Main Queue.</summary>
        public int RetryTtlMs { get; set; } = 5000;

        /// <summary>Số lần retry tối đa trước khi coi là fail vĩnh viễn -> DLQ.</summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>Số message tối đa xử lý song song / chưa ack trên 1 consumer.</summary>
        
    }

    public class RabbitMqConsumerOptions
    {
        public string QueueName { get; set; }
        public ushort PrefetchCount { get; set; } = 10;
    }

    // =====================================================================
    // 2) BASE CLASS DÙNG CHUNG CHO MỌI WORKER
    // =====================================================================
    /// <summary>
    /// Base class chuẩn hóa: khai báo hạ tầng queue, consume, ack/nack, retry + DLQ,
    /// hỗ trợ dừng graceful qua CancellationToken.
    /// Lớp con chỉ cần override ProcessMessageAsync để viết logic nghiệp vụ.
    /// </summary>
    public abstract class RabbitMqWorkerBase
    {
        protected readonly IChannel Channel;
        protected readonly List<RabbitQueueOptions> Options;
        private readonly List<RabbitMqConsumerOptions> _consumerOptions;
        private readonly Action<Exception, string> _logError; // thay bằng ILogger thực tế nếu có

        protected RabbitMqWorkerBase(IChannel channel, 
                                    List<RabbitQueueOptions> options,
                                    List<RabbitMqConsumerOptions> consumerOptions = null, 
                                    Action<Exception, string> logError = null)
        {
            Channel = channel;
            Options = options;
            _consumerOptions = consumerOptions;
            _logError = logError ?? ((ex, msg) => Console.Error.WriteLine($"{msg}: {ex}"));
        }

        /// <summary>
        /// Khai báo hạ tầng queue. Gọi 1 lần khi khởi tạo (KHÔNG block trong constructor
        /// bằng .GetAwaiter().GetResult() như code gốc — dễ deadlock. Gọi await ở nơi
        /// khởi tạo async, ví dụ trong StartAsync của IHostedService).
        ///
        /// Cơ chế:
        /// - DLQ: điểm cuối, không có DLX -> cần người/ tool xử lý thủ công.
        /// - Main Queue: nếu bị reject không rõ lý do sẽ tự rơi vào DLQ (an toàn, phòng hờ).
        /// - Retry Queue: có TTL, hết hạn tự động dead-letter quay lại Main Queue.
        /// </summary>
        public async Task DeclareInfrastructureAsync()
        {
            foreach (var option in Options)
            {
                await Channel.QueueDeclareAsync(
                 queue: option.DeadLetterQueue,
                 durable: true, exclusive: false, autoDelete: false,
                 arguments: null);

                await Channel.QueueDeclareAsync(
                    queue: option.MainQueue,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                    { "x-dead-letter-exchange", option.ExchangeName },
                    { "x-dead-letter-routing-key", option.DeadLetterQueue }
                    });

                await Channel.QueueDeclareAsync(
                    queue: option.RetryQueue,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                    { "x-dead-letter-exchange", option.ExchangeName },
                    { "x-dead-letter-routing-key", option.MainQueue },
                    { "x-message-ttl", option.RetryTtlMs }
                    });
            }
        }

        /// <summary>
        /// Bắt đầu consume. Prefetch + manual ack, hỗ trợ CancellationToken để dừng đúng cách
        /// khi ứng dụng shutdown (thay vì Fire-and-forget không kiểm soát như code gốc).
        /// </summary>
        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            if(_consumerOptions == null || !_consumerOptions.Any())
                throw new InvalidOperationException("No consumer options provided.");   
            foreach (var option in _consumerOptions)
            {
                await Channel.BasicQosAsync(0, option.PrefetchCount, false);

                var consumer = new AsyncEventingBasicConsumer(Channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        await ProcessMessageAsync(ea, cancellationToken);
                        await Channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Utilities.logger.ErrorLog(ex, $"Error on queue {option.QueueName}");
                        await HandleFailureAsync(ea, ex);
                    }
                };

                await Channel.BasicConsumeAsync(option.QueueName, autoAck: false, consumer, cancellationToken);
            }
        }

        /// <summary>Logic nghiệp vụ chính — viết ở lớp con.</summary>
        protected abstract Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken);

        /// <summary>
        /// Đếm retry qua header "x-retry-count" (tự quản lý, thay vì đọc x-death của RabbitMQ
        /// vốn phức tạp khi có nhiều tầng DLX). Dưới ngưỡng -> đẩy Retry Queue.
        /// Vượt ngưỡng -> đẩy thẳng DLQ, kèm lý do lỗi để debug sau này.
        /// </summary>
        protected virtual async Task HandleFailureAsync(BasicDeliverEventArgs ea, Exception ex)
        {
            foreach (var option in Options)
            {
                int retryCount = GetRetryCount(ea.BasicProperties);

                // Luôn ack message gốc để không block queue; việc retry do mình tự publish lại.
                await Channel.BasicAckAsync(ea.DeliveryTag, false);

                var props = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                    Headers = new Dictionary<string, object>
                {
                    { "x-retry-count", retryCount + 1 },
                    { "x-last-error", ex.Message }
                }
                };

                if (retryCount + 1 > option.MaxRetryCount)
                {
                    await Channel.BasicPublishAsync(option.ExchangeName, option.DeadLetterQueue, false, props, ea.Body);
                }
                else
                {
                    await Channel.BasicPublishAsync(option.ExchangeName, option.RetryQueue, false, props, ea.Body);
                }
            }
        }

        private int GetRetryCount(IReadOnlyBasicProperties props)
        {
            if (props?.Headers != null && props.Headers.TryGetValue("x-retry-count", out var val))
            {
                switch (val)
                {
                    case int i: return i;
                    case long l: return (int)l;
                    case byte[] b: return int.Parse(Encoding.UTF8.GetString(b));
                    default: return 0;
                }
            }
            return 0;
        }

        protected  T ProcessMessageToObject<T>(BasicDeliverEventArgs ea)
        {
            var bytedata = ea.Body.ToArray();
            string jsonMessage = Encoding.UTF8.GetString(bytedata);

            //xu ly message
            return jsonMessage.DeserializeJsonTo<T>();
        }
    }

    // =====================================================================
    // 3) VÍ DỤ ÁP DỤNG — tương đương SignHashToKhaiProcess gốc
    // =====================================================================
    public class SampleSignWorker : RabbitMqWorkerBase
    {
        // Inject các service nghiệp vụ như CoreService, CA2SigningService... ở đây
        public SampleSignWorker(IChannel channel)
            : base(channel, new List<RabbitQueueOptions>
            {
                new RabbitQueueOptions
                {
                     MainQueue = "HSCA2.ToKhai.q",
                     RetryQueue = "HSCA2.ToKhai.retry.q",
                     DeadLetterQueue = "HSCA2.ToKhai.dlq",
                     RetryTtlMs = 5000,
                     MaxRetryCount = 3,
                    
                },
            },
                  new List<RabbitMqConsumerOptions>
                {
                    new RabbitMqConsumerOptions
                    {
                        QueueName = "HSCA2.ToKhai.q",
                        PrefetchCount = 10
                    }
                })
        {
        }

        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            // 1. Deserialize message
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            // var hs = JsonSerializer.Deserialize<HoSoMessage>(body);
            // if (hs == null) throw new Exception("Deserialize error or incorrect message");

            // 2. (Khuyến nghị) Kiểm tra idempotency: nếu transactionId đã xử lý xong rồi thì bỏ qua,
            //    tránh xử lý trùng khi RabbitMQ redeliver message do mất kết nối/consumer crash giữa chừng.
            // if (await _coreService.IsAlreadyProcessed(hs.transactionId)) return;

            // 3. Logic nghiệp vụ (ký hash, gọi CA2 service, update DB, v.v.)
            await Task.Delay(10, cancellationToken); // placeholder

            // 4. Publish sang bước tiếp theo (nếu có)
            // await Channel.BasicPublishAsync("", "HSCA2.ReadyToSign.q", false,
            //     new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
            //     hs.GetBytesStringFromJsonObject());
        }
    }

    // =====================================================================
    // 4) VÍ DỤ KHỞI TẠO — nên dùng IHostedService thay vì "_ = worker.RunAsync()"
    // =====================================================================
    public class SampleWorkerHostedService // : IHostedService  (nếu dùng .NET Generic Host)
    {
        private readonly SampleSignWorker _worker;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SampleWorkerHostedService(IChannel channel)
        {
            _worker = new SampleSignWorker(channel);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _worker.DeclareInfrastructureAsync();   // khai báo queue trước
            await _worker.StartConsumingAsync(_cts.Token); // rồi mới consume
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel(); // dừng consumer đúng cách khi app shutdown
            return Task.CompletedTask;
        }
    }
}
