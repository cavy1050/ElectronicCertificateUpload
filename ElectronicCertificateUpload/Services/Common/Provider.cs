using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using ElectronicCertificateUpload.Core;
using HAMS.Frame.Kernel.SecurityService;

namespace ElectronicCertificateUpload.Services
{
    internal class Provider
    {
        static string settingFilePath = AppDomain.CurrentDomain.BaseDirectory + "setting.json";
        static string settingJsonText;
        static JObject settingJsonObj, logSettingJsonObj, dataBaseSettingJsonObj, comstomSettingJsonObj, dictionarySettingJsonObj;
        static List<SelectiondKind> uploadRangeHub, uploadTimePointHub;

        static Provider()
        {
            settingJsonText = File.ReadAllText(settingFilePath);
            settingJsonObj = JObject.Parse(settingJsonText);

            logSettingJsonObj = settingJsonObj.Value<JObject>("LogSetting");
            dataBaseSettingJsonObj = settingJsonObj.Value<JObject>("DataBaseSetting");
            comstomSettingJsonObj = settingJsonObj.Value<JObject>("ComstomSetting");
            dictionarySettingJsonObj = settingJsonObj.Value<JObject>("DictionarySetting");
        }

        internal static string LogFilePath
        {
            get => logSettingJsonObj.Value<string>("LOG_FILE_PATH");
        }

        internal static bool LogEnabledFlag
        {
            get => logSettingJsonObj.Value<bool>("LOG_ENABLED_FLAG");
        }

        internal static string LogLevel
        {
            get => logSettingJsonObj.Value<string>("LOG_LEVEL");
        }   

        internal static string NativeDBConnectString
        {
            get => Connector.DataBaseConnectionStringDecrypt(dataBaseSettingJsonObj.Value<string>("NativeDBConnectString"));
        }

        internal static string MZDBConnectString
        {
            get => Connector.DataBaseConnectionStringDecrypt(dataBaseSettingJsonObj.Value<string>("MZDBConnectString"));
        }

        internal static string ZYDBConnectString
        {
            get => Connector.DataBaseConnectionStringDecrypt(dataBaseSettingJsonObj.Value<string>("ZYDBConnectString"));
        }

        internal static string DZPJDBConnectString
        {
            get => Connector.DataBaseConnectionStringDecrypt(dataBaseSettingJsonObj.Value<string>("DZPJDBConnectString"));
        }

        internal static string SUPNINS_CODE
        {
            get => comstomSettingJsonObj.Value<string>("SUPNINS_CODE");
        }

        internal static string FIXMEDINS_CODE
        {
            get => comstomSettingJsonObj.Value<string>("FIXMEDINS_CODE");
        }

        internal static string FIXMEDINS_NAME
        {
            get => comstomSettingJsonObj.Value<string>("FIXMEDINS_NAME");
        }

        internal static string FIX_BLNG_ADMDVS
        {
            get => comstomSettingJsonObj.Value<string>("FIX_BLNG_ADMDVS");
        }

        internal static List<SelectiondKind> UploadRanges
        {
            get
            {
                JArray uploadRangeArray = dictionarySettingJsonObj.Value<JArray>("UploadRanges");
                uploadRangeHub = uploadRangeArray.ToObject<List<SelectiondKind>>();
                return uploadRangeHub;
            }
        }

        internal static List<SelectiondKind> UploadTimePoints
        {
            get
            {
                JArray uploadTimePointArray = dictionarySettingJsonObj.Value<JArray>("UploadTimePoints");
                uploadTimePointHub = uploadTimePointArray.ToObject<List<SelectiondKind>>();
                return uploadTimePointHub;
            }
        }
    }
}
