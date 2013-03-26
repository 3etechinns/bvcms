using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using CmsData;
using UtilityExtensions;
using System.IO;
using System.Web.Configuration;
using System.Data.SqlClient;
using CmsWeb.Models;
using Rackspace.CloudFiles.Domain;

namespace CmsWeb.Areas.Manage.Controllers
{
    public class AccountController : CmsControllerNoHttps
    {
		[MyRequireHttps]
        [HttpPost]
        public ActionResult KeepAlive()
        {
            return Content("alive");
        }
		[MyRequireHttps]
        [HttpPost]
        public ActionResult CKEditorUpload(string CKEditorFuncNum)
        {
            var m = new AccountModel();
			string baseurl = null;
			if (Request.Files.Count == 0)
				return Content("");
			var file = Request.Files[0];
			var fn = "{0}.{1:yyMMddHHmm}.{2}".Fmt(DbUtil.Db.Host, DateTime.Now, 
				m.CleanFileName(Path.GetFileName(file.FileName)));
			var error = string.Empty;
			var rackspacecdn = ConfigurationManager.AppSettings["RackspaceUrlCDN"];

			if (rackspacecdn.HasValue())
			{
				baseurl = rackspacecdn;
				var username = ConfigurationManager.AppSettings["RackspaceUser"];
				var key = ConfigurationManager.AppSettings["RackspaceKey"];
				var userCreds = new UserCredentials(username, key);
	            var connection = new Rackspace.CloudFiles.Connection(userCreds);
                connection.PutStorageItem("AllFiles", file.InputStream, fn);
			}
			else // local server
			{
				baseurl = "{0}://{1}/Upload/".Fmt(Request.Url.Scheme, Request.Url.Authority);
				try
				{
					string path = Server.MapPath("/Upload/");
					path += fn;

					path = m.GetNewFileName(path);
					file.SaveAs(path);
				}
				catch (Exception ex)
				{
					error = ex.Message;
					baseurl = string.Empty;
				}
			}
            return Content(string.Format(
"<script type='text/javascript'>window.parent.CKEDITOR.tools.callFunction( {0}, '{1}', '{2}' );</script>",
CKEditorFuncNum, baseurl + fn, error));
        }

        public ActionResult ForceError()
        {
            var y = 2;
            var z = 0;
            var x = 2/z;
            return Content("error");
        }

        public ActionResult Error()
        {
            return View();
        }
		[MyRequireHttps]
        public ActionResult LogOn()
        {
		    if (!DbUtil.DatabaseExists())
		        return Redirect("/Errors/DatabaseNotFound.aspx?dbname=" + Util.Host);

		    if (DbUtil.Db.Roles.Any(rr => rr.RoleName == "disabled"))
                    return Content("Site is disabled, contact {0} for help".Fmt(Util.SendErrorsTo()[0].Address));

            if (!User.Identity.IsAuthenticated)
            {
                string user = AccountModel.GetValidToken(Request.QueryString["otltoken"]);
                if (user.HasValue())
                {
                    FormsAuthentication.SetAuthCookie(user, false);
                    AccountModel.SetUserInfo(user, Session);
                    Util.FormsBasedAuthentication = true;
                    var returnUrl = Request.QueryString["returnUrl"];
                    if (returnUrl.HasValue())
                        return Redirect(returnUrl);
                    return Redirect("/");
                }
            }
            return View();
        }
		[MyRequireHttps]
        [HttpPost]
        public ActionResult LogOn(string userName, string password, string returnUrl)
        {
            if (returnUrl.HasValue())
            {
                var lc = returnUrl.ToLower();
                if (lc.StartsWith("/default.aspx") || lc.StartsWith("/login.aspx"))
                    returnUrl = "/";
            }

            if (!userName.HasValue())
                return View();

            var ret = AccountModel.AuthenticateLogon(userName, password, Session, Request);
            if (ret is string)
            {
                ModelState.AddModelError("login", ret.ToString());
                return View();
            }
            var user = ret as User;

            if (user.MustChangePassword)
                return Redirect("/Account/ChangePassword");
            if (!returnUrl.HasValue())
                if (!CMSRoleProvider.provider.IsUserInRole(user.Username, "Access"))
                    return Redirect("/Person/Index/" + Util.UserPeopleId);
            if (returnUrl.HasValue())
                return Redirect(returnUrl);
            return Redirect("/");
        }
		[MyRequireHttps]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
			Session.Abandon();
            return Redirect("/");
        }
		[MyRequireHttps]
        public ActionResult ForgotUsername(string email)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
                return View();

            if (!Util.ValidEmail(email))
                ModelState.AddModelError("email", "valid email required");
            if (!ModelState.IsValid)
                return View();

            if (email != null)
                email = email.Trim();
            var q = from u in DbUtil.Db.Users
                    where u.Person.EmailAddress == email || u.Person.EmailAddress2 == email
                    where email != "" && email != null
                    select u;
            foreach (var user in q)
            {
                DbUtil.Db.EmailRedacted(DbUtil.AdminMail, user.Person, "bvcms forgot username", @"Hi {0},
<p>Your username is: {1}</p>
<p>If you did not request this, please disregard this message.</p>
<p>Thanks,<br />
The bvCMS Team</p>
".Fmt(user.Name, user.Username));
                DbUtil.Db.SubmitChanges();
                DbUtil.Db.EmailRedacted(DbUtil.AdminMail,
                    CMSRoleProvider.provider.GetAdmins(),
                    "bvcms user: {0} forgot username".Fmt(user.Name), "no content");
            }
            if (q.Count() == 0)
                DbUtil.Db.EmailRedacted(DbUtil.AdminMail,
                    CMSRoleProvider.provider.GetAdmins(),
                    "bvcms unknown email: {0} forgot username".Fmt(email), "no content");

            return RedirectToAction("RequestUsername");

        }
		[MyRequireHttps]
        public ActionResult ForgotPassword(string username)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
                return View();
            if (!username.HasValue())
                ModelState.AddModelError("username", "user name required");
            if (!ModelState.IsValid)
                return View();
            var email = AccountModel.ForgotPassword(username);
            TempData["email"] = email;

            return RedirectToAction("RequestPassword");
        }
		[MyRequireHttps]
        public ActionResult CreateAccount(string id)
        {
            if (!id.HasValue())
				return Content("invalid URL"); 

            var pid = AccountModel.GetValidToken(id).ToInt();
            var p = DbUtil.Db.LoadPersonById(pid);
            if (p == null)
                return View("LinkUsed");
		    var minage = DbUtil.Db.Setting("MinimumUserAge", "16").ToInt();
		    if ((p.Age ?? 16) < minage)
                return Content("must be Adult ({0} or older)".Fmt(minage));
            var user = MembershipService.CreateUser(DbUtil.Db, pid);
            FormsAuthentication.SetAuthCookie(user.Username, false);
            AccountModel.SetUserInfo(user.Username, Session);

        	ViewBag.user = user.Username;
			ViewBag.MinPasswordLength = MembershipService.MinPasswordLength;
			ViewBag.RequireSpecialCharacter = MembershipService.RequireSpecialCharacter;
			ViewBag.RequireOneNumber = MembershipService.RequireOneNumber;
			ViewBag.RequireOneUpper = MembershipService.RequireOneUpper;

            Util.FormsBasedAuthentication = true;
            return View("SetPassword");
        }
		[MyRequireHttps]
        public ActionResult RequestPassword()
        {
            return View();
        }
		[MyRequireHttps]
        public ActionResult RequestUsername()
        {
            return View();
        }
		[MyRequireHttps]
        [Authorize]
        public ActionResult ChangePassword()
        {
            ViewBag.MinPasswordLength = MembershipService.MinPasswordLength;
        	ViewBag.RequireSpecialCharacter = MembershipService.RequireSpecialCharacter;
        	ViewBag.RequireOneNumber = MembershipService.RequireOneNumber;
        	ViewBag.RequireOneUpper = MembershipService.RequireOneUpper;
            return View();
        }
		[MyRequireHttps]
        [HttpGet]
        public ActionResult SetPassword(Guid? id)
        {
            if (!id.HasValue)
                return Content("invalid URL");
            var user = DbUtil.Db.Users.SingleOrDefault(u => u.ResetPasswordCode == id);
            if (user == null || (user.ResetPasswordExpires.HasValue && user.ResetPasswordExpires < DateTime.Now))
                return View("LinkUsed");
            user.ResetPasswordCode = null;
			user.IsLockedOut = false;
			user.FailedPasswordAttemptCount = 0;
            DbUtil.Db.SubmitChanges();
            FormsAuthentication.SetAuthCookie(user.Username, false);
            AccountModel.SetUserInfo(user.Username, Session);
            ViewBag.user = user.Username;
            ViewBag.MinPasswordLength = MembershipService.MinPasswordLength;
        	ViewBag.RequireSpecialCharacter = MembershipService.RequireSpecialCharacter;
        	ViewBag.RequireOneNumber = MembershipService.RequireOneNumber;
        	ViewBag.RequireOneUpper = MembershipService.RequireOneUpper;
            Util.FormsBasedAuthentication = true;
            return View();
        }
		[MyRequireHttps]
        [HttpPost]
        [Authorize]
        public ActionResult SetPassword(string newPassword, string confirmPassword)
        {
            ViewBag.user = User.Identity.Name;
            ViewBag.MinPasswordLength = MembershipService.MinPasswordLength;
        	ViewBag.RequireSpecialCharacter = MembershipService.RequireSpecialCharacter;
        	ViewBag.RequireOneNumber = MembershipService.RequireOneNumber;
        	ViewBag.RequireOneUpper = MembershipService.RequireOneUpper;

            if (!ValidateChangePassword("na", newPassword, confirmPassword))
                return View();
            var mu = CMSMembershipProvider.provider.GetUser(User.Identity.Name, false);
            mu.UnlockUser();
            try
            {
                if (mu.ChangePassword(mu.ResetPassword(), newPassword))
                    return RedirectToAction("ChangePasswordSuccess");
                else
                    ModelState.AddModelError("form", "The current password is incorrect or the new password is invalid.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("form", ex.Message);
            }
            return View();
        }
		[MyRequireHttps]
        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            ViewBag.user = User.Identity.Name;
            ViewBag.MinPasswordLength = MembershipService.MinPasswordLength;
        	ViewBag.RequireSpecialCharacter = MembershipService.RequireSpecialCharacter;
        	ViewBag.RequireOneNumber = MembershipService.RequireOneNumber;
        	ViewBag.RequireOneUpper = MembershipService.RequireOneUpper;

            if (!ValidateChangePassword(currentPassword, newPassword, confirmPassword))
                return View();

            try
            {
                if (MembershipService.ChangePassword(User.Identity.Name, currentPassword, newPassword))
                    return RedirectToAction("ChangePasswordSuccess");
                else
                {
                    ModelState.AddModelError("form", "The current password is incorrect or the new password is invalid.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("form", ex.Message);
                return View();
            }
        }
		[MyRequireHttps]
        public ActionResult ChangePasswordSuccess()
        {
        	var rd = DbUtil.Db.Setting("RedirectAfterPasswordChange", "");
			if (rd.HasValue())
				return Redirect(rd);
            return View();
        }
        private bool ValidateChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword))
                ModelState.AddModelError("currentPassword", "You must specify a current password.");
            if (newPassword == null || newPassword.Length < MembershipService.MinPasswordLength)
                ModelState.AddModelError("newPassword",
                        String.Format(CultureInfo.CurrentCulture,
                             "You must specify a new password of {0} or more characters.",
                             MembershipService.MinPasswordLength));
            if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                ModelState.AddModelError("form", "The new password and confirmation password do not match.");
            return ModelState.IsValid;
        }
        private bool ValidateLogOn(string userName, string password)
        {
            if (!MembershipService.ValidateUser(userName, password))
                ModelState.AddModelError("login", "Username or password not recognized.");
            return ModelState.IsValid;
        }
    }
}
