using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Multiuse
{
    internal class GetSPOProperty
    {
        private readonly LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;



        // TO BE DEPRECATED FOR VERSION 0.4.0
        internal GetSPOProperty(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal void CSOMSingle(ClientObject clientObject, string property)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMSingle";
            //_logHelper.AddLogToTxt(methodName, $"Start getting List '{listName}' from Site '{siteUrl}'");

            string[] properties = { property };

            CSOMMultiple(clientObject, properties);

        }

        internal void CSOMMultiple(ClientObject clientObject, string[] properties)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMMultiple";
            //_logHelper.AddLogToTxt(methodName, $"Start getting List '{listName}' from Site '{siteUrl}'");




            var loadRequired = false;
            foreach (var property in properties)
            {
                var expression = GetClientObjectExpression(clientObject, property);

                if (!clientObject.IsPropertyAvailable(expression))
                {
                    clientObject.Context.Load(clientObject, expression);
                    loadRequired = true;
                }
            }
            if (loadRequired)
            {
                clientObject.Context.ExecuteQueryRetry();
            }
        }

        private static Expression<Func<ClientObject, object>> GetClientObjectExpression(ClientObject clientObject, string property)
        {
            var memberExpression = Expression.PropertyOrField(Expression.Constant(clientObject), property);
            var memberName = memberExpression.Member.Name;

            var parameter = Expression.Parameter(typeof(ClientObject), "i");
            var cast = Expression.Convert(parameter, memberExpression.Member.ReflectedType);
            var body = Expression.Property(cast, memberName);
            var exp = Expression.Lambda<Func<ClientObject, Object>>(Expression.Convert(body, typeof(object)), parameter);

            return exp;

        }
    }
}
