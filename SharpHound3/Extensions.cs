﻿using System;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Security.Principal;
using System.Text;
using SharpHound3.Enums;

namespace SharpHound3
{
    internal static class Extensions
    {
        public static void PrintEntry(this SearchResultEntry searchResultEntry)
        {
            foreach (var propertyName in searchResultEntry.Attributes.AttributeNames)
            {
                var property = propertyName.ToString();
                Console.WriteLine(property);
                Console.WriteLine(searchResultEntry.GetProperty(property));
            }
        }

        #region DirectoryEntry

        public static string GetSid(this DirectoryEntry directoryEntry)
        {
            if (!directoryEntry.Properties.Contains("objectsid"))
                return null;

            var sid = directoryEntry.Properties["objectsid"][0];

            switch (sid)
            {
                case byte[] b:
                    return new SecurityIdentifier(b, 0).Value;
                case string st:
                    return new SecurityIdentifier(Encoding.ASCII.GetBytes(st), 0).Value;
            }

            return null;
        }

        public static string GetProperty(this DirectoryEntry directoryEntry, string property)
        {
            if (!directoryEntry.Properties.Contains(property))
                return null;

            return directoryEntry.Properties[property][0].ToString();
        }

        public static LdapTypeEnum GetLdapType(this DirectoryEntry directoryEntry)
        {
            var objectSid = directoryEntry.GetSid();
            if (CommonPrincipal.GetCommonSid(objectSid, out var commonPrincipal))
            {
                return commonPrincipal.Type;
            }

            var objectType = LdapTypeEnum.Unknown;
            var samAccountType = directoryEntry.GetProperty("samaccounttype");

            if (samAccountType != null)
            {
                if (samAccountType == "805306370")
                    return LdapTypeEnum.Unknown;

                objectType = Helpers.SamAccountTypeToType(samAccountType);
            }
            else
            {
                var objectClasses = directoryEntry.GetPropertyAsArray("objectClass");
                if (objectClasses == null)
                {
                    objectType = LdapTypeEnum.Unknown;
                }
                else if (objectClasses.Contains("groupPolicyContainer"))
                {
                    objectType = LdapTypeEnum.GPO;
                }
                else if (objectClasses.Contains("organizationalUnit"))
                {
                    objectType = LdapTypeEnum.OU;
                }
                else if (objectClasses.Contains("domain"))
                {
                    objectType = LdapTypeEnum.Domain;
                }
            }

            return objectType;
        }

        public static string[] GetPropertyAsArray(this DirectoryEntry directoryEntry, string property)
        {
            if (!directoryEntry.Properties.Contains(property))
                return new string[0];

            var values = directoryEntry.Properties[property];

            var propArray = new string[values.Count];

            for (var i = 0; i < values.Count; i++)
                propArray[i] = values[i].ToString();

            return propArray;
        }

        #endregion

        #region SearchResultEntry
        public static string GetProperty(this SearchResultEntry searchResultEntry, string property)
        {
            if (!searchResultEntry.Attributes.Contains(property))
                return null;

            return searchResultEntry.Attributes[property][0].ToString();
        }

        public static string GetSid(this SearchResultEntry searchResultEntry)
        {
            if (!searchResultEntry.Attributes.Contains("objectsid"))
                return null;

            //objectsid can sometimes be either a string or a byte array. Just AD things
            var s = searchResultEntry.Attributes["objectsid"][0];
            switch (s)
            {
                case byte[] b:
                    return new SecurityIdentifier(b, 0).Value;
                case string st:
                    return new SecurityIdentifier(Encoding.ASCII.GetBytes(st), 0).Value;
            }

            return null;
        }

        public static string[] GetPropertyAsArray(this SearchResultEntry searchResultEntry, string property)
        {
            if (!searchResultEntry.Attributes.Contains(property))
                return new string[0];

            var values = searchResultEntry.Attributes[property];

            var propArray = new string[values.Count];

            for (var i = 0; i < values.Count; i++)
                propArray[i] = values[i].ToString();

            return propArray;
        }

        public static byte[] GetPropertyAsBytes(this SearchResultEntry searchResultEntry, string property)
        {
            if (!searchResultEntry.Attributes.Contains(property))
                return null;
            return searchResultEntry.Attributes[property][0] as byte[];
        }

        public static string GetObjectIdentifier(this SearchResultEntry searchResultEntry)
        {
            if (!searchResultEntry.Attributes.Contains("objectsid") &&
                !searchResultEntry.Attributes.Contains("objectguid"))
                return null;

            if (searchResultEntry.Attributes.Contains("objectsid"))
            {
                return searchResultEntry.GetSid();
            }

            var guidBytes = searchResultEntry.GetPropertyAsBytes("objectguid");
            return new Guid(guidBytes).ToString();
        }

        /// <summary>
        /// Extension method to determine the type of a SearchResultEntry.
        /// Requires objectsid, samaccounttype, objectclass
        /// </summary>
        /// <param name="searchResultEntry"></param>
        /// <returns></returns>
        public static LdapTypeEnum GetLdapType(this SearchResultEntry searchResultEntry)
        {
            var objectSid = searchResultEntry.GetSid();
            if (CommonPrincipal.GetCommonSid(objectSid, out var commonPrincipal))
            {
                return commonPrincipal.Type;
            }

            var objectType = LdapTypeEnum.Unknown;
            var samAccountType = searchResultEntry.GetProperty("samaccounttype");
            //Its not a common principal. Lets use properties to figure out what it actually is
            if (samAccountType != null)
            {
                if (samAccountType == "805306370")
                    return LdapTypeEnum.Unknown;

                objectType = Helpers.SamAccountTypeToType(samAccountType);
            }
            else
            {
                var objectClasses = searchResultEntry.GetPropertyAsArray("objectClass");
                if (objectClasses == null)
                {
                    objectType = LdapTypeEnum.Unknown;
                }
                else if (objectClasses.Contains("groupPolicyContainer"))
                {
                    objectType = LdapTypeEnum.GPO;
                }
                else if (objectClasses.Contains("organizationalUnit"))
                {
                    objectType = LdapTypeEnum.OU;
                }
                else if (objectClasses.Contains("domain"))
                {
                    objectType = LdapTypeEnum.Domain;
                }
            }
            return objectType;
        }

        #endregion

        public static bool HasFlag(this Enum self, Enum test)
        {
            if (self == null || test == null)
            {
                return false;
            }

            try
            {
                var temp = Convert.ToInt32(self);
                var num = Convert.ToInt32(test);
                return (temp & num) == num;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
