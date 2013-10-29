using KladrApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RealEstate.SmartProcessing
{
    public class KladrApi
    {
        const string token = @"526e8b0631608fa055000001";
        const string key = @"0f2dd7e8d4adec22ff3fcc0294c8a4cf45bab633";

        private readonly KladrClient kladrClient;

        public KladrApi()
        {
            kladrClient = new KladrClient(token, key);
        }

        AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
        KladrResponse _response = null;

        public string GetDistinct(string city, string address)
        {
            if (String.IsNullOrEmpty(city) || String.IsNullOrEmpty(address))
                return null;

            city = System.Web.HttpUtility.HtmlEncode(city);
            address = System.Web.HttpUtility.HtmlEncode(address);

            var call = new KladrClient.KladrApiCallback(fetchedAddress);
            kladrClient.FindAddress(new Dictionary<string, string>
                                        {
                                            {"query", city},
                                            {"contentType", "city"},
                                            {"withParent", "1"},
                                            {"limit", "1"}
                                        }, call);

            stopWaitHandle.WaitOne();

            string id = null;
            if (_response != null)
                if (_response.result != null && _response.result.Count() > 0)
                    id = _response.result[0].id;

            if (id == null) return null;


            var parts = address.Split(' ');
            var max = parts.Max(s => s.Length);
            address = parts.First(s => s.Length == max).Trim(new char[] { ',', '.' }).Trim();

            stopWaitHandle.Reset();
            kladrClient.FindAddress(new Dictionary<string, string>
                                        {
                                            {"query", address},
                                            {"contentType", "street"},
                                            {"cityId", id},
                                            {"withParent", "1"},
                                            {"limit", "1"}
                                        }, call);

            stopWaitHandle.WaitOne();

            id = null;
            if (_response != null)
                if (_response.result != null && _response.result.Count() > 0)
                    id = _response.result[0].id;

            if (id == null) return null;

            stopWaitHandle.Reset();
            kladrClient.FindAddress(new Dictionary<string, string>
                                        {
                                            {"query", "1"},
                                            {"contentType", "building"},
                                            {"streetId", id},
                                            {"withParent", "1"},
                                            {"limit", "1"}
                                        }, call);

            stopWaitHandle.WaitOne();

            if (_response != null)
                if (_response.result != null && _response.result.Count() > 0)
                    id = _response.result[0].id;

            return null;

        }

        private void fetchedAddress(KladrResponse response)
        {
            _response = response;
            stopWaitHandle.Set();
        }
    }
}
