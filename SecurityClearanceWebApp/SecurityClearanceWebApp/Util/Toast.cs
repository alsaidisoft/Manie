namespace APP.Util
{
    public class Toast
    {
        public string message { get; set; }
        public string title { get; set; } = "";
        public string icon { get; set; } = "fa fa-info";
        public string color { get; set; } = "green";
        public string position { get; set; } = "center";
        public int timeout { get; set; } = 1500;
        public bool rtl { get; set; } = true;
        public string progressBarColor { get; set; } = "rgba(255, 254, 253, 0.6)";
        public string messageColor { get; set; } = "#ffffff";
        public string titleColor { get; set; } = "#ffffff";
        public string theme { get; set; } = "dark";
        public int MessageSize { get; set; } = 18;

        public Toast(string title, string message, string color)
        {
            this.title = title;
            this.message = message;
            this.color = color;
        }


    }
}