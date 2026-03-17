using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS;
using LP.WXK.K3.Common.Utils;

namespace LP.WXK.K3.App.ServicePlugIn
{
    /// <summary>
    /// OA同步服务类
    /// 提供OA系统认证、跳转节点同步等功能
    /// </summary>
    public class OASyncService
    {
        private readonly string baseURL = "http://10.10.100.34:81";
        private readonly string appId = "f975a20b-8632-4b0a-9be7-342b010be988";
        private string secrit = "";
        private string spk = "";
        private readonly HttpClient httpClient;
        private readonly RsaHelper rsaHelper;

        public OASyncService()
        {
            httpClient = new HttpClient();
            rsaHelper = new RsaHelper();
        }

        /// <summary>
        /// 同步OA跳转节点
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>是否成功</returns>
        public bool skipCurrentCodeAsync(Context context, string requestId)
        {
            try
            {
                return SkipCurrentCodeAsyncInternal(context, requestId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                saveOALog(context, requestId, "", $"Exception: {ex.Message}", false);
                return false;
            }
        }

        private async Task<bool> SkipCurrentCodeAsyncInternal(Context context, string requestId)
        {

            string url = $"{baseURL}/api/xfd/skipCurrentNode?requestId={requestId}";
            string secret = regist();
            string token = applyToken(secret);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("appid", appId);
            httpRequest.Headers.Add("token", token);
            string userid = rsaHelper.EncryptByPublicKey("1", spk);
            httpRequest.Headers.Add("userid", userid);

            using (HttpResponseMessage response = await httpClient.SendAsync(httpRequest))
            {
                using (HttpContent content = response.Content)
                {
                    var responseContent = await content.ReadAsStringAsync();
                    JSONObject json = null;

                    try
                    {
                        json = JSONObject.Parse(responseContent);
                    }
                    catch (Exception)
                    {
                        saveOALog(context, requestId, url, responseContent, false);
                        return false;
                    }

                    saveOALog(context, requestId, url, Convert.ToString(json), true);

                    if (json.ContainsKey("code"))
                    {
                        string code = Convert.ToString(json["code"]);
                        if ("200".Equals(code))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 注册OA
        /// </summary>
        /// <returns>加密后的secret</returns>
        public string regist()
        {
            try
            {
                return RegistInternal().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return "";
            }
        }

        private async Task<string> RegistInternal()
        {
            string url = baseURL + "/api/ec/dev/auth/regist";

            if (!string.IsNullOrEmpty(secrit) && !string.IsNullOrEmpty(spk))
            {
                string secret = rsaHelper.EncryptByPublicKey(secrit, spk);
                return secret;
            }

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("appid", appId);

            using (HttpResponseMessage response = await httpClient.SendAsync(httpRequest))
            {
                using (HttpContent content = response.Content)
                {
                    var responseContent = await content.ReadAsStringAsync();
                    JSONObject json = null;

                    try
                    {
                        json = JSONObject.Parse(responseContent);
                    }
                    catch (Exception)
                    {
                        return "";
                    }

                    if (json.ContainsKey("status") && Convert.ToBoolean(json["status"]))
                    {
                        if (json.ContainsKey("secrit"))
                            secrit = Convert.ToString(json["secrit"]);
                        if (json.ContainsKey("spk"))
                            spk = Convert.ToString(json["spk"]);

                        string secret = rsaHelper.EncryptByPublicKey(secrit, spk);
                        return secret;
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// 获取Token
        /// </summary>
        /// <param name="secret">加密后的secret</param>
        /// <returns>Token字符串</returns>
        public string applyToken(string secret)
        {
            try
            {
                return ApplyTokenInternal(secret).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return "";
            }
        }

        private async Task<string> ApplyTokenInternal(string secret)
        {
            string url = baseURL + "/api/ec/dev/auth/applytoken";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("appid", appId);
            httpRequest.Headers.Add("secret", secret);

            using (HttpResponseMessage response = await httpClient.SendAsync(httpRequest))
            {
                using (HttpContent content = response.Content)
                {
                    var responseContent = await content.ReadAsStringAsync();
                    JSONObject json = null;

                    try
                    {
                        json = JSONObject.Parse(responseContent);
                    }
                    catch (Exception)
                    {
                        return "";
                    }

                    if (json.ContainsKey("status") && Convert.ToBoolean(json["status"]))
                    {
                        if (json.ContainsKey("token"))
                        {
                            string token = Convert.ToString(json["token"]);
                            return token;
                        }
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// 保存OA日志
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="number">单据编号</param>
        /// <param name="req">请求内容</param>
        /// <param name="resp">响应内容</param>
        /// <param name="isSuccess">是否成功</param>
        public void saveOALog(Context context, string number, string req, string resp, bool isSuccess)
        {
            try
            {
                IMetaDataService metadataService = ServiceHelper.GetService<IMetaDataService>();
                FormMetadata meta = metadataService.Load(context, "TWLG_OASyncLog") as FormMetadata;

                if (meta == null)
                {
                    return;
                }

                ISaveService saveService = ServiceHelper.GetService<ISaveService>();

                DynamicObject oaSyncLog = meta.BusinessInfo.GetDynamicObjectType().CreateInstance() as DynamicObject;
                if (oaSyncLog != null)
                {
                    oaSyncLog["BillNo"] = number;
                    oaSyncLog["TWLG_Request"] = req;
                    oaSyncLog["TWLG_Response"] = resp;
                    oaSyncLog["TWLG_Success"] = isSuccess;
                    oaSyncLog["TWLG_CreateDate"] = DateTime.Now;
                    oaSyncLog["TWLG_SyncDate"] = DateTime.Now;

                    DynamicObject[] objects = new DynamicObject[] { oaSyncLog };
                    saveService.Save(context, objects);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
