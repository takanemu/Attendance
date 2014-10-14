
namespace MonoRaspberryPi
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Kintone
    {
        string id;
        string pass;
        string host;

        /// <summary>
        /// エンコーディング
        /// </summary>
        private Encoding enc = Encoding.GetEncoding("UTF-8");

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="pass">パスワード</param>
        /// <param name="host">ホストアドレス</param>
        public Kintone(string id, string pass, string host)
        {
            this.id = id;
            this.pass = pass;
            this.host = host;
        }

        /// <summary>
        /// 認証コード文字列生成
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="pass">パスワード</param>
        /// <returns>生成文字列</returns>
        protected string XCybozuAuthorizationBase64(string id, string pass)
        {
            return Convert.ToBase64String(enc.GetBytes(id + ":" + pass));
        }

        /// <summary>
        /// カード番号文字列生成
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>生成文字列</returns>
        protected string IDmUrlEncode(string idm)
        {
            return "IDm" + Uri.EscapeUriString(" = \"" + idm + "\" ");
        }

        /// <summary>
        /// 日付文字列生成
        /// </summary>
        /// <returns>生成文字列</returns>
        protected string DateUrlEncode()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");

            return Uri.EscapeUriString(" 日付 = \"" + date + "\"");
        }

        /// <summary>
        /// レコード番号文字列生成
        /// </summary>
        /// <returns>生成文字列</returns>
        protected string Fields1UrlEncode()
        {
            return "&fields" + Uri.EscapeUriString("[0]=レコード番号");
        }

        /// <summary>
        /// 休憩積算文字列生成
        /// </summary>
        /// <returns>生成文字列</returns>
        protected string Fields2UrlEncode()
        {
            return "&fields" + Uri.EscapeUriString("[1]=休憩積算");
        }

        /// <summary>
        /// REST(GET)の通信の実施
        /// </summary>
        /// <returns></returns>
        protected async Task<string> ExecuteRestGet(string idm)
        {
            Console.WriteLine("REST(GET)の通信の実施");
            string resultString = "";

            UriBuilder ub = new UriBuilder("https", this.host, 443, @"k/v1/records.json", "?app=17&query=" + this.IDmUrlEncode(idm) + "and" + this.DateUrlEncode() + this.Fields1UrlEncode() + this.Fields2UrlEncode());

            HttpClientHandler handler = new HttpClientHandler();

            HttpClient cl = new HttpClient(handler);
            cl.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cl.DefaultRequestHeaders.Add("X-Cybozu-Authorization", this.XCybozuAuthorizationBase64(this.id, this.pass));
            cl.DefaultRequestHeaders.Add("Host", ub.Host + ":" + ub.Port);

            HttpResponseMessage httpResponse = null;
            try
            {
                Console.WriteLine("GetAsync(" + ub.Uri.AbsoluteUri + ")");
                httpResponse = await cl.GetAsync(ub.Uri);
            }
            catch(Exception ex)
            {
                Console.WriteLine("GetAsync:" + ex.Message);
                throw;
            }

            // サーバエラー(404NotFoundや500InternalErrorなど)の場合は、WebExceptionをスローする
            if (!httpResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("サーバーエラー:" + httpResponse.ReasonPhrase.ToString());
                throw new WebException(httpResponse.ReasonPhrase.ToString());
            }
            resultString = await httpResponse.Content.ReadAsStringAsync();

            return resultString;
        }

        /// <summary>
        /// 指定レコード情報の取得
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>レコード情報</returns>
        public async Task<KintaiRecords> ReadAttendanceRecord(string idm)
        {
            Console.WriteLine("レコード情報の取得");
            string result = await this.ExecuteRestGet(idm);

            KintaiRecords entity = new KintaiRecords();

            try
            {
                dynamic resultJson = JValue.Parse(result);
                JArray array = resultJson.records;

                if (array.Count > 0)
                {
                    // レコードが存在するので、休憩か退勤
                    entity.IsExist = true;

                    JObject theItem = (JObject)array.First;
                    KintaiItem theData = theItem.ToObject<KintaiItem>();

                    entity.RecordNo = theData.RecordNo.value;

                    JArray sekisanArray = (JArray)theItem["休憩積算"]["value"];

                    if (sekisanArray.Count > 0)
                    {
                        JObject theSekisanObject = (JObject)sekisanArray[0]["value"];

                        entity.RestStartTime = theSekisanObject["休憩開始"].ToObject<KintaiItemValue>().value;
                        entity.RestEndTime = theSekisanObject["休憩終了"].ToObject<KintaiItemValue>().value;
                    }
                }
            }
            catch (Exception ex)
            {
                Exception exception = new Exception(ex.Message);
                throw exception;
            }
            return entity;
        }

        /// <summary>
        /// 出勤打刻用JSON作成
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>JSON文字列</returns>
        protected string MakeCreateRecord(string idm)
        {
            CreateKintai entity = new CreateKintai();

            entity.record = new CreateKintaiRecord();

            entity.record.IDm = new KintaiItemValue("SINGLE_LINE_TEXT", idm);
            entity.record.出勤時刻 = new KintaiItemValue("TIME", DateTime.Now.ToString("HH:mm"));

            string json = JsonConvert.SerializeObject(entity);

            return json;
        }

        /// <summary>
        /// 出勤打刻
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> CreateAttendanceRecord(string idm)
        {
            UriBuilder ub = new UriBuilder("https", this.host, 443, @"k/v1/record.json");

            HttpClientHandler handler = new HttpClientHandler();

            HttpClient cl = new HttpClient(handler);
            cl.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cl.DefaultRequestHeaders.Add("X-Cybozu-Authorization", this.XCybozuAuthorizationBase64(this.id, this.pass));
            cl.DefaultRequestHeaders.Add("Host", ub.Host + ":" + ub.Port);

            string json = this.MakeCreateRecord(idm);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponse = null;
            try
            {
                httpResponse = await cl.PostAsync(ub.Uri, content);
            }
            catch
            {
                throw;
            }
            if (!httpResponse.IsSuccessStatusCode)
            {
                KintaiResult result = new KintaiResult
                {
                    IsResult = false
                };
                return result;
            }
            string resultString = await httpResponse.Content.ReadAsStringAsync();

            KintaiResult resultJson = JValue.Parse(resultString).ToObject<KintaiResult>();

            return resultJson;
        }

        /// <summary>
        /// 退勤打刻用JSON作成
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>JSON文字列</returns>
        protected string MakeClockingOut(string recordNo)
        {
            ClockingOutKintai entity = new ClockingOutKintai();

            entity.id = recordNo;
            entity.record = new ClockingOutKintaiRecord();

            entity.record.退勤時刻 = new KintaiItemValue("TIME", DateTime.Now.ToString("HH:mm"));

            string json = JsonConvert.SerializeObject(entity);

            return json;
        }

        /// <summary>
        /// 休憩開始JSON文字列作成
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>JSON文字列</returns>
        protected string MakeRestStart(string recordNo)
        {
            string json = "{\"app\":17,\"id\":\"@record@\",\"record\":{\"休憩積算\":{\"value\":{\"0\":{\"value\":{\"休憩開始\":{\"value\":\"@time@\"},}},}}}}";

            json = System.Text.RegularExpressions.Regex.Replace(json, "@record@", recordNo);
            json = System.Text.RegularExpressions.Regex.Replace(json, "@time@", DateTime.Now.ToString("HH:mm"));

            return json;
        }

        /// <summary>
        /// 休憩戻りJSON文字列作成
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <param name="start">休憩開始時刻</param>
        /// <returns>JSON文字列</returns>
        protected string MakeRestEnd(string recordNo, string start)
        {
            string json = "{\"app\":17,\"id\":\"@record@\",\"record\":{\"休憩積算\":{\"value\":{\"0\":{\"value\":{\"休憩戻り\":{\"value\":\"@time2@\"},\"休憩開始\":{\"value\":\"@time1@\"},}},}}}}";

            json = System.Text.RegularExpressions.Regex.Replace(json, "@record@", recordNo);
            json = System.Text.RegularExpressions.Regex.Replace(json, "@time1@", start);
            json = System.Text.RegularExpressions.Regex.Replace(json, "@time2@", DateTime.Now.ToString("HH:mm"));

            return json;
        }

        /// <summary>
        /// Put処理
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>結果</returns>
        protected async Task<KintaiResult> KintaiPutAsync(string json)
        {
            UriBuilder ub = new UriBuilder("https", this.host, 443, @"k/v1/record.json");

            HttpClientHandler handler = new HttpClientHandler();

            HttpClient cl = new HttpClient(handler);
            cl.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cl.DefaultRequestHeaders.Add("X-Cybozu-Authorization", this.XCybozuAuthorizationBase64(this.id, this.pass));
            cl.DefaultRequestHeaders.Add("Host", ub.Host + ":" + ub.Port);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponse = null;
            try
            {
                httpResponse = await cl.PutAsync(ub.Uri, content);
            }
            catch
            {
                throw;
            }
            if (!httpResponse.IsSuccessStatusCode)
            {
                KintaiResult result = new KintaiResult
                {
                    IsResult = false
                };
                return result;
            }
            string resultString = await httpResponse.Content.ReadAsStringAsync();

            KintaiResult resultJson = JValue.Parse(resultString).ToObject<KintaiResult>();

            return resultJson;
        }

        /// <summary>
        /// 休憩開始打刻
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> RestStart(string recordNo)
        {
            string json = this.MakeRestStart(recordNo);

            KintaiResult result = await KintaiPutAsync(json);

            return result;
        }

        /// <summary>
        /// 休憩戻り打刻
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <param name="start"></param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> RestEnd(string recordNo, string start)
        {
            string json = this.MakeRestEnd(recordNo, start);

            KintaiResult result = await KintaiPutAsync(json);

            return result;
        }

        /// <summary>
        /// 退勤打刻
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> ClockingOut(string recordNo)
        {
            string json = this.MakeClockingOut(recordNo);

            KintaiResult result = await KintaiPutAsync(json);

            return result;
        }
    }
}
