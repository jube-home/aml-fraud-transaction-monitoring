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

namespace Jube.Engine.EntityAnalysisModelInvoke.Helpers
{
    using System;
    using System.Net;
    using System.Net.Mail;
    using System.Threading;
    using System.Threading.Tasks;
    using DynamicEnvironment;
    using log4net;

    public static class SendMail
    {
        public static async Task<bool> SendAsync(
            string toEmail,
            string subject,
            string body,
            ILog log,
            DynamicEnvironment jubeEnvironment,
            CancellationToken token = default)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"SMTP Client: Preparing to send message to {toEmail} with subject '{subject}'.");
                }

                using var smtpServer = new SmtpClient();

                if (jubeEnvironment.AppSettings("SMTPUseDefaultCredentials") == null ||
                    jubeEnvironment.AppSettings("SMTPUser") == null ||
                    jubeEnvironment.AppSettings("SMTPPassword") == null)
                {
                    log.Info("SMTP Client: No SMTP credentials specified.");

                    return false;
                }

                smtpServer.UseDefaultCredentials = jubeEnvironment.AppSettings("SMTPUseDefaultCredentials")
                    .Equals("True", StringComparison.OrdinalIgnoreCase);

                smtpServer.Credentials = new NetworkCredential(
                    jubeEnvironment.AppSettings("SMTPUser"),
                    jubeEnvironment.AppSettings("SMTPPassword"));

                if (jubeEnvironment.AppSettings("SMTPPort") == null || jubeEnvironment.AppSettings("SMTPHost") == null)
                {
                    log.Info("SMTP Client: No SMTP host and port specified.");

                    return false;
                }

                smtpServer.Port = Int32.Parse(jubeEnvironment.AppSettings("SMTPPort"));
                smtpServer.Host = jubeEnvironment.AppSettings("SMTPHost");

                smtpServer.EnableSsl = jubeEnvironment.AppSettings("SMTPEnableSsl")
                    .Equals("True", StringComparison.OrdinalIgnoreCase);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"SMTP Client: Configured SMTP server with Host={smtpServer.Host}, Port={smtpServer.Port}, EnableSsl={smtpServer.EnableSsl}, User={jubeEnvironment.AppSettings("SMTPUser")}.");
                }

                using var email = new MailMessage();
                email.From = new MailAddress(jubeEnvironment.AppSettings("SMTPFrom"));
                email.Subject = subject;
                email.Body = body;
                email.IsBodyHtml = false;
                email.To.Add(toEmail);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"SMTP Client: Email compiled. To={toEmail}, Subject='{subject}', Body length={body.Length}.");
                }

                var taskCompletionSource = new TaskCompletionSource<object>();

                await using (token.Register(() => taskCompletionSource.TrySetCanceled()).ConfigureAwait(false))
                {
                    smtpServer.SendCompleted += (_, e) =>
                    {
                        if (e.Cancelled)
                        {
                            taskCompletionSource.TrySetCanceled();
                        }
                        else if (e.Error != null)
                        {
                            taskCompletionSource.TrySetException(e.Error);
                        }
                        else
                        {
                            taskCompletionSource.TrySetResult(null);
                        }
                    };

                    smtpServer.SendAsync(email, null);

                    await taskCompletionSource.Task.ConfigureAwait(false);
                }

                if (log.IsInfoEnabled)
                {
                    log.Info("SMTP Client: Message has been sent successfully.");
                }

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"SMTP Client: Failed to send message. Exception: {ex}");

                return false;
            }
        }
    }
}