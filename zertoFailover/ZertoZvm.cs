using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace zertoFailover
{
    public class ZertoZvm
    {
        //Zerto ZVM API - REST v1

        private static string zvm_Version = "1.0";
        private static string zvm_ContentTypeValue = "application/json";
        private static string zvm_Session;
        private static string zvm_BaseUrl;

        private static Dictionary<string, string> Parameters = new Dictionary<string, string>();
        private static String BuildURLParametersString(Dictionary<string, string> parameters)
        {
            UriBuilder uriBuilder = new UriBuilder();
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var urlParameter in parameters)
            {
                query[urlParameter.Key] = urlParameter.Value;
            }
            uriBuilder.Query = query.ToString();
            return uriBuilder.Query;
        }

        public static bool GetSession(string baseUrl, string username, string password, string contentType)
        {
            try
            {
                var url = "/v1/session/add";
                var credentials = Encoding.ASCII.GetBytes(username + ":" + password);
                var data = new StringContent("", Encoding.UTF8, contentType);

                ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl)
                };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
                var response = client.PostAsync(url, data);
                if (response.Result.IsSuccessStatusCode)
                {
                    zvm_BaseUrl = baseUrl;
                    zvm_Session = response.Result.Headers.GetValues("x-zerto-session").FirstOrDefault().ToString();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static JToken GetResult(string url, Dictionary<string, string> urlParameters)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

            var client = new HttpClient
            {
                BaseAddress = new Uri(zvm_BaseUrl)
            };
            client.DefaultRequestHeaders.Add("x-zerto-session", zvm_Session);
            String parameters = BuildURLParametersString(urlParameters);
            var response = client.GetAsync(url + parameters);
            var content = response.Result.Content.ReadAsStringAsync();
            var result = JToken.Parse(content.Result.ToString());
            return result;
        }

        private static string PostRequest(string url, JToken request)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

            var client = new HttpClient
            {
                BaseAddress = new Uri(zvm_BaseUrl)
            };
            client.DefaultRequestHeaders.Add("x-zerto-session", zvm_Session);
            var serializer = new JavaScriptSerializer();
            var content = new StringContent(request.ToString(), Encoding.UTF8, zvm_ContentTypeValue);
            var result = client.PostAsync(url, content).Result.Content.ReadAsStringAsync().Result.ToString();
            return result;
        }

        public static string GetVpgSettingsIdentifier(JToken body)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.PostRequest("/v1/vpgSettings", body);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgSettingsObject(string VpgSettingsIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/vpgSettings/" + VpgSettingsIdentifier, Parameters).ToString(); ;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string CommitVpgSettingsObject(string VpgSettingsIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.PostRequest("/v1/vpgSettings/" + VpgSettingsIdentifier + "/commit", "");
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetLocalSiteIdentifier()
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/localsite", Parameters).SelectToken("SiteIdentifier").ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetSites()
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetNetworks(string TargetSiteIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/networks", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetClusters(string TargetSiteIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/hostclusters", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetDatastores(string TargetSiteIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/datastores", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetFolders(string TargetSiteIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/folders", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetOrgVdcs(string TargetSiteIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/orgvdcs", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetOrgVdcNetworks(string TargetSiteIdentifier, string OrgVdcIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/orgvdcs/" + OrgVdcIdentifier + "/networks", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetOrgVdcStorageProfiles(string TargetSiteIdentifier, string OrgVdcIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/orgvdcs/" + OrgVdcIdentifier + "/storageprofiles", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetServiceProfiles()
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/serviceprofiles", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgSettings(string VpgSettingsIdentifier)
        {
            Parameters.Clear();
            try
            {
                if (VpgSettingsIdentifier == null)
                {
                    return ZertoZvm.GetResult("/v1/vpgSettings", Parameters).ToString();
                }
                else
                {
                    return ZertoZvm.GetResult("/v1/vpgSettings/" + VpgSettingsIdentifier, Parameters).ToString();
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVms(string TargetSiteIdentifier)
        {
            Parameters.Clear();
            try
            {
                return ZertoZvm.GetResult("/v1/virtualizationsites/" + TargetSiteIdentifier + "/vms", Parameters).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static string GetVpgId(string vpgName)
        {
            Parameters.Clear();
            try
            {
                if (vpgName.Length > 0) { Parameters.Add("name", vpgName); }
                return ZertoZvm.GetResult("/v1/vpgs", Parameters).SelectToken("[0].VpgIdentifier").ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public static JToken FailoverTest(string vpgName, string vmName)
        {
            Parameters.Clear();
            try
            {
                if (vpgName.Length > 0) { Parameters.Add("vpgName", vpgName); }
                if (vmName.Length > 0) { Parameters.Add("vmName", vmName); }

                JArray vms = (JArray)(ZertoZvm.GetResult("/v1/vms", Parameters));
                var vpgId = vms.SelectToken("[0].VpgIdentifier");
                var checkpointId = ZertoZvm.GetResult("/v1/vpgs/" + vpgId + "/checkpoints/stats", Parameters).SelectToken("Latest.CheckpointIdentifier");
                JArray VmIdentifiers = new JArray();
                foreach (var vm in vms)
                {
                    VmIdentifiers.Add(vm.SelectToken("VmIdentifier"));
                }
                JToken request = new JObject(
                    new JProperty("CheckpointIdentifier", checkpointId),
                    new JProperty("VmIdentifiers", VmIdentifiers));

                var result = JToken.Parse(ZertoZvm.PostRequest("/v1/vpgs/" + vpgId + "/FailoverTest", request));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken FailoverTestStop(string vpgName)
        {
            try
            {
                var vpgId = GetVpgId(vpgName);
                var result = JToken.Parse(ZertoZvm.PostRequest("/v1/vpgs/" + vpgId + "/FailoverTestStop", ""));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken Failover(string vpgName, string vmName, int commitPolicy, int waitTime)
        {
            Parameters.Clear();
            try
            {
                if (vpgName.Length > 0) { Parameters.Add("vpgName", vpgName); }
                if (vmName.Length > 0) { Parameters.Add("vmName", vmName); }

                JArray vms = (JArray)(ZertoZvm.GetResult("/v1/vms", Parameters));
                var vpgId = vms.SelectToken("[0].VpgIdentifier");
                var checkpointId = ZertoZvm.GetResult("/v1/vpgs/" + vpgId + "/checkpoints/stats", Parameters).SelectToken("Latest.CheckpointIdentifier");
                JArray VmIdentifiers = new JArray();
                foreach (var vm in vms)
                {
                    VmIdentifiers.Add(vm.SelectToken("VmIdentifier"));
                }
                JToken request = new JObject(
                    new JProperty("CheckpointIdentifier", checkpointId),
                    new JProperty("CommitPolicy", commitPolicy),
                    new JProperty("ShutdownPolicy", 2),
                    new JProperty("TimeToWaitBeforeShutdownInSec", waitTime),
                    new JProperty("IsReverseProtection", false),
                    new JProperty("VmIdentifiers", VmIdentifiers));

                var result = JToken.Parse(ZertoZvm.PostRequest("/v1/vpgs/" + vpgId + "/Failover", request));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken FailoverCommit(string vpgName)
        {
            try
            {
                var vpgId = GetVpgId(vpgName);

                JToken request = new JObject(
                    new JProperty("IsReverseProtection", false));

                var result = JToken.Parse(ZertoZvm.PostRequest("/v1/vpgs/" + vpgId + "/FailoverCommit", ""));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static JToken FailoverRollback(string vpgName)
        {
            try
            {
                var vpgId = GetVpgId(vpgName);
                var result = JToken.Parse(ZertoZvm.PostRequest("/v1/vpgs/" + vpgId + "/FailoverRollback", ""));
                return result;
            }
            catch (Exception e)
            {
                return JToken.Parse(e.ToString());
            }
        }

        public static int TaskStatus(string taskId)
        {
            Parameters.Clear();
            try
            {
                var taskStatus = Convert.ToInt32(ZertoZvm.GetResult("/v1/tasks/" + taskId, Parameters).SelectToken("Status.State"));
                return taskStatus;
            }
            catch
            {
                return -1;
            }
        }

        public static bool TaskComplete(string taskId)
        {
            Parameters.Clear();
            try
            {
                var taskStatus = Convert.ToInt32(ZertoZvm.GetResult("/v1/tasks/" + taskId, Parameters).SelectToken("Status.State"));
                if (taskStatus == 4 || taskStatus == 5 || taskStatus == 6)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return true;                //returns true if an error occurs to avoid any processes waiting for the task to finish
            }

        }
    }
}
