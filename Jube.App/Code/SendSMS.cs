/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License 
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty  
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not, 
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using log4net;

namespace Jube.App.Code
{
    public class SendSms
    {
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly ILog _log;

        public SendSms(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        {
            _dynamicEnvironment = dynamicEnvironment;
            _log = log;
        }

        public void Send(string notificationDestination, string notificationBody)
        {
            var clickatellString
                = $"https://platform.clickatell.com/messages/http/send?apiKey={_dynamicEnvironment.AppSettings("ClickatellAPIKey")}&to={HttpUtility.UrlEncode(notificationDestination.Replace("+", "").Replace(" ", ""))}&content={HttpUtility.UrlEncode(notificationBody)}";
            try
            {
                var client = new HttpClient();
                var response = client.GetAsync(clickatellString);

                var valueTask = Task.Run(()=> response.Result.Content.ReadAsStringAsync());
                valueTask.Wait();

                _log.Info($"Notification Dispatch: Result is {valueTask.Result}.");
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Notification Dispatch: Has failed to send Clickatell string of {clickatellString} with error of {ex}.");
            }
        }
    }
}