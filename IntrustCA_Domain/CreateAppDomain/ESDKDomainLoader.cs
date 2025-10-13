using IntrustCA_Domain.CreateAppDomain;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace IntrustCA_Domain.CreateAppDomain
{
    internal class ESDKDomainLoader : MarshalByRefObject
    {
        private Assembly _eSDKAssembly;

        /// <summary>
        /// Khoi tao loader: load rms.lib.common.dll + eSDK.dll, set path config
        /// trong appDomain này khi resolve thi chi lay  thu muc eSDKRuntimePath
        /// </summary>
        /// <param name="eSDKRuntimePath">Thư mục chứa eSDK.dll, rms.lib.common.dll và ConfigRMS</param>
        public void Initialize(string eSDKRuntimePath)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("itextsharp", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"[MAIN DOMAIN] Loaded iTextSharp: {asm.FullName}");
            }

            if (!Directory.Exists(eSDKRuntimePath))
                throw new DirectoryNotFoundException($"Không tìm thấy thư mục eSDKRuntime: {eSDKRuntimePath}");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string name = new AssemblyName(args.Name).Name + ".dll";
                string path = Path.Combine(eSDKRuntimePath, name);
                if (File.Exists(path))
                    return Assembly.LoadFrom(path);
                return null;
            };

            // Load rms.lib.common.dll
            string rmsLibPath = Path.Combine(eSDKRuntimePath, "rms.lib.common.dll");
            if (!File.Exists(rmsLibPath))
                throw new FileNotFoundException("Không tìm thấy rms.lib.common.dll", rmsLibPath);

            var rmsAssembly = Assembly.LoadFrom(rmsLibPath);

            // Set _pathToFileConfig
            var publibType = rmsAssembly.GetType("rms.lib.common.library.Publib");
            if (publibType != null)
            {
                var field = publibType.GetField("_pathToFileConfig", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    string configPath = Path.Combine(eSDKRuntimePath, "ConfigRMS");
                    field.SetValue(null, configPath);
                }
            }

            // Load eSDK.dll
            string eSDKPath = Path.Combine(eSDKRuntimePath, "eSDK.dll");
            if (!File.Exists(eSDKPath))
                throw new FileNotFoundException("Không tìm thấy eSDK.dll", eSDKPath);

            _eSDKAssembly = Assembly.LoadFrom(eSDKPath);
        }


        /// <summary>
        /// Goi method cua eSDK
        /// </summary>
        /// <param name="methodName">Tên method</param>
        /// <param name="jsonData">Dữ liệu JSON</param>
        /// <returns>Kết quả JSON</returns>
        public string InvokeSigner(string methodName, string jsonData)
        {
            var signerType = _eSDKAssembly.GetType("eSDK.Signer");
            if (signerType == null)
                throw new Exception("Không tìm thấy class eSDK.Signer trong eSDK.dll");

            var method = signerType.GetMethod(methodName);
            if (method == null)
                throw new Exception($"Không tìm thấy hàm '{methodName}' trong eSDK.Signer");
            object result = null;
            if (methodName == "signRMS")
            {
                 result = method.Invoke(methodName, new object[] { jsonData, false });
            }
            else
            {
                result = method.Invoke(null, new object[] { jsonData });
            }
            return result?.ToString();
        }
    }

    internal class ESDKCaller
    {
        public static string Call(string methodName, string jsonData)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string binPath = Path.Combine(baseDir, "bin");
            //if (Directory.Exists(binPath))
            //{
            //    baseDir = binPath;
            //}
            var setup = new AppDomainSetup
            {
                ApplicationBase = baseDir,
                PrivateBinPath = "eSDKRuntime",
            };

            var domain = AppDomain.CreateDomain("ESDK_Domain", null, setup);
            try
            {
                var loaderType = typeof(ESDKDomainLoader);
                var loader = (ESDKDomainLoader)domain.CreateInstanceAndUnwrap(
                    loaderType.Assembly.FullName,
                    loaderType.FullName
                );
                // Đường dẫn eSDK.dll
                string eSDKPath = Path.Combine(baseDir, "eSDKRuntime");
                loader.Initialize(eSDKPath);
                return loader.InvokeSigner(methodName, jsonData);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi gọi eSDK: " + ex.Message, ex);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        //public static string Call(string methodName, string jsonData)
        //{
        //    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        //    string runtimeDir = Path.Combine(baseDir, "eSDKRuntime");

        //    var setup = new AppDomainSetup
        //    {
        //        ApplicationBase = baseDir,
        //        PrivateBinPath = "eSDKRuntime",
        //        ShadowCopyFiles = "true"
        //    };

        //    Console.WriteLine($"[DEBUG] BaseDir: {baseDir}");
        //    Console.WriteLine($"[DEBUG] IntrustCA_Domain.dll exists? {File.Exists(Path.Combine(baseDir, "IntrustCA_Domain.dll"))}");
        //    Console.WriteLine($"[DEBUG] eSDKRuntime exists? {Directory.Exists(runtimeDir)}");

        //    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        //    {
        //        string asmName = new AssemblyName(args.Name).Name + ".dll";

        //        string localPath = Path.Combine(baseDir, asmName);
        //        if (File.Exists(localPath))
        //            return Assembly.LoadFrom(localPath);

        //        string runtimePath = Path.Combine(runtimeDir, asmName);
        //        if (File.Exists(runtimePath))
        //            return Assembly.LoadFrom(runtimePath);

        //        return null;
        //    };

        //    var domain = AppDomain.CreateDomain("ESDK_Domain", null, setup);

        //    try
        //    {
        //        var loaderType = typeof(ESDKDomainLoader);
        //        var loader = (ESDKDomainLoader)domain.CreateInstanceAndUnwrap(
        //            loaderType.Assembly.GetName().Name,
        //            loaderType.FullName
        //        );

        //        return loader.InvokeSigner(methodName, jsonData);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Lỗi khi gọi eSDK: " + ex, ex);
        //    }
        //    finally
        //    {
        //        AppDomain.Unload(domain);
        //    }
        //}
    }
}
