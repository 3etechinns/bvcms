/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Web;
using System.Configuration;

namespace UtilityExtensions
{
    public static partial class Util
    {
        public static string UserName
        {
            get
            {
                if (HttpContext.Current != null)
                    return GetUserName(HttpContext.Current.User.Identity.Name);
                return ConfigurationManager.AppSettings["TestName"];
            }
        }
        private const string STR_UserId = "UserId";
        public static int UserId
        {
            get
            {
                int id = 0;
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[STR_UserId] != null)
                            id = HttpContext.Current.Session[STR_UserId].ToInt();
                if (id == 0)
                    id = ConfigurationManager.AppSettings["TestId"].ToInt();
                return id;
            }
            set
            {
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        HttpContext.Current.Session[STR_UserId] = value;
            }
        }
        private const string STR_UserPreferredName = "UserPreferredName";
        public static string UserPreferredName
        {
            get
            {
                string name = null;
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[STR_UserPreferredName] != null)
                            name = HttpContext.Current.Session[STR_UserPreferredName] as String;
                return name;
            }
            set
            {
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        HttpContext.Current.Session[STR_UserPreferredName] = value;
            }
        }
        private const string STR_UserFullName = "UserFullName";
        public static string UserFullName
        {
            get
            {
                string name = "-";
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[STR_UserFullName] != null)
                            name = HttpContext.Current.Session[STR_UserFullName] as String;
                return name;
            }
            set
            {
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        HttpContext.Current.Session[STR_UserFullName] = value;
            }
        }

        private const string UserFirstNameSessionKey = "UserFirstName";
        public static string UserFirstName
        {
            get
            {
                string name = string.Empty;
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[UserFirstNameSessionKey] != null)
                            name = HttpContext.Current.Session[UserFirstNameSessionKey] as String;
                return name;
            }
            set
            {
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        HttpContext.Current.Session[UserFirstNameSessionKey] = value;
            }
        }

        public static int UserId1
        {
            get { return UserId == 0 ? 1 : UserId; }
        }
        private const string STR_UserPeopleId = "UserPeopleId";
        public static int? UserPeopleId
        {
            get
            {
                int? id = null;
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[STR_UserPeopleId] != null)
                            id = HttpContext.Current.Session[STR_UserPeopleId].ToInt();
                return id;
            }
            set
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Session[STR_UserPeopleId] = value;
            }
        }

        private const string UserThumbPictureSessionKey = "UserThumbPictureUrl";
        public static string UserThumbPictureUrl
        {
            get
            {
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[UserThumbPictureSessionKey] != null)
                            return (string)HttpContext.Current.Session[UserThumbPictureSessionKey];
                return string.Empty;
            }
            set
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Session[UserThumbPictureSessionKey] = value;
            }
        }

        private const string UserThumbPictureBgPosSessionKey = "UserThumbPictureBgPosition";
        public static string UserThumbPictureBgPosition
        {
            get
            {
                if (HttpContext.Current != null)
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[UserThumbPictureBgPosSessionKey] != null)
                            return (string)HttpContext.Current.Session[UserThumbPictureBgPosSessionKey];
                return string.Empty;
            }
            set
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Session[UserThumbPictureBgPosSessionKey] = value;
            }
        }

        public static string GetUserName(string name)
        {
            if (name == null)
                return null;
            var a = name.Split('\\');
            if (a.Length == 2)
                return a[1];
            return a[0];
        }
        public const string STR_Preferences = "Preferences";
        public const string STR_PageSize = "PageSize";
        public static void SetPageSizeCookie(int value)
        {
            var cookie = new HttpCookie(STR_Preferences);
            cookie.Values[STR_PageSize] = value.ToString();
            cookie.Expires = DateTime.MaxValue;
            HttpContext.Current.Response.AppendCookie(cookie);
        }
        public static int GetPageSizeCookie()
        {
            HttpRequest r = null;
            if (HttpContext.Current != null)
                r = HttpContext.Current.Request;
            if (r != null && r.Cookies[STR_Preferences] != null)
            {
                var cookie = r.Cookies[STR_Preferences];
                if (cookie != null && cookie.Values[STR_PageSize] != null)
                    return cookie.Values[STR_PageSize].ToInt();
            }
            return 10;
        }
    }
}

