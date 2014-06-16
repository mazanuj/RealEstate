using KladrApiClient;
using RealEstate.OKATO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            if(address.Contains('/'))
            {
                var ind = address.IndexOf('/');
                address = address.Substring(0, ind);
            }

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


            var parts = address.Split(' ').Where(s => !s.Contains("просп")
                && !s.Contains("шосс") && !s.Contains("улиц")
                && !s.Contains("тракт") && !s.Contains("проезд") && !s.Contains("переул")).ToList();

            var max = parts.Max(s => s.Length);
            address = parts.First().Trim(new char[] { ',', '.' }).Trim();

            var addrParts = address.Split('.');
            if(addrParts.Count() > 0)
            {
                address = addrParts.Max(c => c);
            }

            Regex r = new Regex(@"\d+");
            var number = parts.FirstOrDefault(p => r.IsMatch(p)) ?? "1";

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
                                            {"query", number},
                                            {"contentType", "building"},
                                            {"streetId", id},
                                            {"withParent", "1"},
                                            {"limit", "1"}
                                        }, call);

            stopWaitHandle.WaitOne();

            string buildId = null;
            if (_response != null)
                if (_response.result != null && _response.result.Count() > 0)
                    buildId = _response.result[0].id;

            if (buildId == null)
            {
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
                        buildId = _response.result[0].id;

                if(buildId == null)
                {
                    stopWaitHandle.Reset();
                    kladrClient.FindAddress(new Dictionary<string, string>
                                        {
                                            {"query", "2"},
                                            {"contentType", "building"},
                                            {"streetId", id},
                                            {"withParent", "1"},
                                            {"limit", "1"}
                                        }, call);

                    stopWaitHandle.WaitOne();

                    if (_response != null)
                        if (_response.result != null && _response.result.Count() > 0)
                            buildId = _response.result[0].id;
                }
            }

            if (buildId == null) return null;

            KLADRDriver dr = new KLADRDriver();
            var okato = dr.GetOKATO(buildId);

            OKATODriver ok = new OKATODriver();
            return ok.GetDistinctByCode(okato);

        }

        public string GetAO(string distinct)
        {
            OKATODriver ok = new OKATODriver();

            var code = ok.GetCodeByDistinct(distinct);

            while (code != null && code.Length > 5)
            {
                code = ok.GetParrentCode(code);
            }

            if (code != null && code.Length == 5)
            {
                return ok.GetDistinctByCode(code);
            }
            else
                return null;
        }

        private void fetchedAddress(KladrResponse response)
        {
            _response = response;
            stopWaitHandle.Set();
        }
    }
}
