using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client.Pages.Account
{
	public partial class Login 
	{

		private LoginModel loginModel = new();
		private async Task HandleValidSubmit()
		{
			if(loginModel.Username == "admin" && loginModel.Password == "password")
			{
				Console.WriteLine("Login Successful!");
			}
			else
			{
				Console.WriteLine("Invalid Username or Password");
			}
		}
		public class LoginModel
		{
			[Required(ErrorMessage = "Please input username")]
			public string Username { get; set; } = string.Empty;

			[Required(ErrorMessage = "Please input password")]
			public string Password { get; set; } = string.Empty;

			public bool RememberMe { get; set; }
		}

	}

}
