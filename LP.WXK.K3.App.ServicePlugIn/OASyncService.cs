using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS;

namespace LP.WXK.K3.App.ServicePlugIn
{
    public class OASyncService
    {
        // private readonly string baseURL = "http://10.10.100.34:81";
        private readonly string baseURL = "http://172.17.14.93:80";
        private readonly string appId = "f975a20b-8632-4b0a-9be7-342b010be988";
        private string secrit = "";
        private string spk = "";
        private readonly HttpClient httpClient;
        private readonly Program p;

        public OASyncService()
        {
            httpClient = new HttpClient();
            p = new Program();
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
                // 记录异常日志
                saveOALog(context, requestId, "", $"Exception: {ex.Message}", false);
                return false;
            }
        }

        private async Task<bool> SkipCurrentCodeAsyncInternal(Context context, string requestId)
        {

            string url = $"{baseURL}/api/xfd/skipCurrentNode?requestId={requestId}";
            string secret = regist();
            string token = applyToken(secret);

            // 构建请求头部
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("appid", appId);
            httpRequest.Headers.Add("token", token);
            string userid = p.EncryptByPublicKey("1", spk);
            httpRequest.Headers.Add("userid", userid);

            // 发送请求
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
                        string code = Convert.ToString(json["code"]);// 业务响应码（200成功，非200失败）
                        if ("200".Equals(code))
                        {
                            // 解析响应
                            return true;
                        }
                    }
                }
            }

            // 解析响应
            return false;
        }

        /// <summary>
        /// 注册OA
        /// </summary>
        /// <returns></returns>
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

            // 如果secrit 和 spk 不为空，则无需重新获取
            if (!string.IsNullOrEmpty(secrit) && !string.IsNullOrEmpty(spk))
            {
                string secret = p.EncryptByPublicKey(secrit, spk);
                return secret;
            }

            // 构建请求头部
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("appid", appId);

            // 发送请求
            using (HttpResponseMessage response = await httpClient.SendAsync(httpRequest))
            {
                using (HttpContent content = response.Content)
                {
                    var responseContent = await content.ReadAsStringAsync();
                    // "{\"msg\":\"ok\",\"code\":0,\"msgShowType\":\"none\",\"secrit\":\"de7bcc05-4fbf-4219-b77f-45633c1a89fd\",\"secret\":\"de7bcc05-4fbf-4219-b77f-45633c1a89fd\",\"status\":true,\"spk\":\"MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwYQGhnEldgHyWEBrAbSa5DKNB2uSxMwoFCredg06JJrWEksh2OYxXUA2hzaG1uM3cYsZYVw3eL1ZJI8wqrZlgqNj4ctAAt9vRosllyXNCtBLXYatMqNF4//+KsoO3M4dvyeVw3SfAx8IAfH544d8el4F88eXdrezCP2tXTxo46PbqzC7EmxGwdTQgMrD0K9YhO7Q3qFx8NTbOjvvcOBWEeisIrUfAxa2txXt+KRd7B0N7bFchKGa8SKlmhWUi0Bpr/HhREJmPu6lT6to7AanyDjhGXe1YM3hmQWlcIMkLnVrtJSV+5hBJVIY/Bo1sfbG39/jR+YffgR6u6aubOsIuQIDAQAB\"}"
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

                        string secret = p.EncryptByPublicKey(secrit, spk);
                        return secret;
                    }
                }
            }

            // 解析响应
            return "";
        }

        /// <summary>
        /// 获取Token
        /// </summary>
        /// <returns></returns>
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

            // 构建请求头部
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("appid", appId);
            httpRequest.Headers.Add("secret", secret);

            // 发送请求
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

            // 解析响应
            return "";
        }

        /// <summary>
        /// 保存OA日志
        /// </summary>
        /// <param name="context"></param>
        /// <param name="number"></param>
        /// <param name="req"></param>
        /// <param name="resp"></param>
        /// <param name="isSuccess"></param>
        /// <returns></returns>
        public void saveOALog(Context context, string number, string req, string resp, bool isSuccess)
        {
            try
            {
                // 获取元数据服务
                IMetaDataService metadataService = ServiceHelper.GetService<IMetaDataService>();
                FormMetadata meta = metadataService.Load(context, "TWLG_OASyncLog") as FormMetadata;

                if (meta == null)
                {
                    return; // 元数据加载失败，直接返回
                }

                // 获取保存服务
                ISaveService saveService = ServiceHelper.GetService<ISaveService>();

                // 创建单据
                DynamicObject oaSyncLog = meta.BusinessInfo.GetDynamicObjectType().CreateInstance() as DynamicObject;
                if (oaSyncLog != null)
                {
                    oaSyncLog["BillNo"] = number;// 单据编号
                    oaSyncLog["TWLG_Request"] = req;// 请求
                    oaSyncLog["TWLG_Response"] = resp;// 响应
                    oaSyncLog["TWLG_Success"] = isSuccess;// 是否成功
                    oaSyncLog["TWLG_CreateDate"] = DateTime.Now;// 创建时间
                    oaSyncLog["TWLG_SyncDate"] = DateTime.Now;// 同步时间

                    DynamicObject[] objects = new DynamicObject[] { oaSyncLog };
                    saveService.Save(context, objects);
                }
            }
            catch (Exception)
            {
                // 避免日志记录失败影响主流程
            }
        }
    }
}
