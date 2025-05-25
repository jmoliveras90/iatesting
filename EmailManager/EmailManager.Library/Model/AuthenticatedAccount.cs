using EmailManager.Library.Enum;

namespace EmailManager.Library.Model
{
    public class AuthenticatedAccount
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public Provider Provider { get; set; } 
    }

}
