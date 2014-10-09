
namespace MonoRaspberryPi
{
    using System.Runtime.Serialization;

    /// <summary>
    /// 設定ファイル・エンティティ
    /// </summary>
    [DataContract]
    public class ApplicationConfig
    {
        /// <summary>
        /// ID
        /// </summary>
        [DataMember]
        public string id;

        /// <summary>
        /// パスワード
        /// </summary>
        [DataMember]
        public string password;

        /// <summary>
        /// ホストアドレス
        /// </summary>
        [DataMember]
        public string host;
    }

    /// <summary>
    /// 勤怠レコードクラス
    /// </summary>
    public class KintaiRecords
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KintaiRecords()
        {
            this.IsExist = false;
        }

        /// <summary>
        /// レコードの有無
        /// </summary>
        public bool IsExist { get; set; }

        /// <summary>
        /// レコード番号
        /// </summary>
        public string RecordNo { get; set; }

        /// <summary>
        /// 休憩開始時間(nullなら出勤のみ)
        /// </summary>
        public string RestStartTime { get; set; }

        /// <summary>
        /// 休憩戻り時間(休憩開始時間と同じなら休憩中)
        /// </summary>
        public string RestEndTime { get; set; }
    }

    /// <summary>
    /// 結果・エンティティ
    /// </summary>
    [DataContract]
    public class KintaiResult
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KintaiResult()
        {
            this.IsResult = true;
        }

        /// <summary>
        /// レコード番号
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// リビジョン番号
        /// </summary>
        [DataMember(Name = "revision")]
        public int Revision { get; set; }

        /// <summary>
        /// 成功フラグ
        /// </summary>
        public bool IsResult { get; set; }
    }

    /// <summary>
    /// 出勤打刻・エンティティ
    /// </summary>
    [DataContract]
    public class CreateKintai
    {
        /// <summary>
        /// アプリケーション番号
        /// </summary>
        [DataMember]
        public int app = 17;

        /// <summary>
        /// レコード
        /// </summary>
        [DataMember]
        public CreateKintaiRecord record;
    }

    /// <summary>
    /// 退勤打刻・レコード
    /// </summary>
    [DataContract]
    public class ClockingOutKintai
    {
        /// <summary>
        /// アプリケーション番号
        /// </summary>
        [DataMember]
        public int app = 17;

        /// <summary>
        /// レコード番号
        /// </summary>
        [DataMember]
        public string id;

        /// <summary>
        /// レコード
        /// </summary>
        [DataMember]
        public ClockingOutKintaiRecord record;
    }

    /// <summary>
    /// 出勤打刻レコード
    /// </summary>
    public class CreateKintaiRecord
    {
        /// <summary>
        /// カード番号
        /// </summary>
        [DataMember(Name = "IDm")]
        public KintaiItemValue IDm { get; set; }

        /// <summary>
        /// 出勤時刻
        /// </summary>
        [DataMember(Name = "出勤時刻")]
        public KintaiItemValue 出勤時刻 { get; set; }
    }

    /// <summary>
    /// 退勤打刻レコード
    /// </summary>
    public class ClockingOutKintaiRecord
    {
        /// <summary>
        /// 退勤打刻
        /// </summary>
        [DataMember(Name = "退勤時刻")]
        public KintaiItemValue 退勤時刻 { get; set; }
    }

    /// <summary>
    /// 勤怠データ・エンティティ
    /// </summary>
    [DataContract]
    public class KintaiItem
    {
        /// <summary>
        /// レコード番号
        /// </summary>
        [DataMember(Name = "レコード番号")]
        public KintaiItemValue RecordNo { get; set; }
    }

    /// <summary>
    /// バリュー・エンティティ
    /// </summary>
    [DataContract]
    public class KintaiItemValue
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="type">型</param>
        /// <param name="value">値</param>
        public KintaiItemValue(string type, string value)
        {
            this.type = type;
            this.value = value;
        }

        /// <summary>
        /// 型
        /// </summary>
        [DataMember(Name = "type")]
        public string type { get; set; }

        /// <summary>
        /// 文字列
        /// </summary>
        [DataMember(Name = "value")]
        public string value { get; set; }
    }
}
