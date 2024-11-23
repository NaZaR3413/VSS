using static web_backend.Blazor.Client.Pages.Account.Register;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace web_backend.Blazor.Client.Pages.Account
{
	public partial class Register : LayoutComponentBase
	{
		private RegisterModel registerModel = new();
		protected override void OnInitialized()
		{
			registerModel.checkedTerms = false;
		}
		private async Task HandleValidSubmit()
		{

		}

		public class RegisterModel
		{

			[Required(ErrorMessage = "* Please enter your first name")]
			public string FirstName { get; set; } = string.Empty;

			[Required(ErrorMessage = "* Please enter your last name")]
			public string LastName { get; set; } = string.Empty;

			[Required(ErrorMessage = "* Please enter a user name")]
			public string Username { get; set; } = string.Empty;

			[Required(ErrorMessage = "* Please enter your email")]
			public string Email { get; set; } = string.Empty;

			[Required(ErrorMessage = "* Please input a password")]
			public string Password { get; set; } = string.Empty;

			[Required(ErrorMessage = "* Please confirm password")]
			public string ConfirmPassword { get; set; } = string.Empty;

			public bool checkedTerms { get; set; }

		}
	}
}
