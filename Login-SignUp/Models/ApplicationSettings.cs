namespace Login_SignUp.Models
{
    public class ApplicationSettings
    {
        public string JWT_Secret { get; set; }
        public string Client_URL { get; set; }
        public string Host_Url { get; set; }
        
    }

    public class EmailSettings
 {
        public string FromMailAddress { get; set; }
        public string Host { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
    }
}