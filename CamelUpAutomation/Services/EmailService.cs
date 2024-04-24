﻿using System;
using MailKit.Security;
using System.Text;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using CamelUpAutomation.Models.Email;
using CamelUpAutomation.Models.Configuration;
using System.Linq;
using RazorEngineCore;
using Microsoft.Extensions.Configuration;
using CamelUpAutomation.Functions.Authentication;

namespace CamelUpAutomation.Services
{ 
    public interface IEmailService
    {
        Task<bool> SendAsync(MailData mailData, CancellationToken ct);
        string GetEmailTemplate(string emailTemplate, TemplateData emailTemplateModel);

    }

    public class EmailService : IEmailService
    {
        private readonly MailSettings _settings;

        public EmailService(IConfiguration config)
        {
            _settings = new MailSettings
            {
                DisplayName = config.GetValue<string>("MailDisplayName"),
                From = config.GetValue<string>("MailFrom"),
                UserName = config.GetValue<string>("MailUserName"),
                Password = config.GetValue<string>("MailPassword"),
                Host = config.GetValue<string>("MailHost"),
                Port = config.GetValue<int>("MailPort"),
                UseSSL = config.GetValue<bool>("MailUseSSL"),
                UseStartTls = config.GetValue<bool>("MailUseStartTls"),
            };
        }

        public async Task<bool> SendAsync(MailData mailData, CancellationToken ct)
        {
            try
            {
                // Initialize a new instance of the MimeKit.MimeMessage class
                var mail = new MimeMessage();

                #region Sender / Receiver
                // Sender
                mail.From.Add(new MailboxAddress(_settings.DisplayName, mailData.From ?? _settings.From));
                mail.Sender = new MailboxAddress(mailData.DisplayName ?? _settings.DisplayName, mailData.From ?? _settings.From);

                // Receiver
                foreach (string mailAddress in mailData.To)
                    mail.To.Add(MailboxAddress.Parse(mailAddress));

                // Set Reply to if specified in mail data
                if (!string.IsNullOrEmpty(mailData.ReplyTo))
                    mail.ReplyTo.Add(new MailboxAddress(mailData.ReplyToName, mailData.ReplyTo));

                // BCC
                // Check if a BCC was supplied in the request
                if (mailData.Bcc != null)
                {
                    // Get only addresses where value is not null or with whitespace. x = value of address
                    foreach (string mailAddress in mailData.Bcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                        mail.Bcc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }

                // CC
                // Check if a CC address was supplied in the request
                if (mailData.Cc != null)
                {
                    foreach (string mailAddress in mailData.Cc.Where(x => !string.IsNullOrWhiteSpace(x)))
                        mail.Cc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }
                #endregion

                #region Content

                // Add Content to Mime Message
                var body = new BodyBuilder();
                mail.Subject = mailData.Subject;
                body.HtmlBody = mailData.Body;
                mail.Body = body.ToMessageBody();

                #endregion

                #region Send Mail

                using var smtp = new SmtpClient();

                if (_settings.UseSSL)
                {
                    await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.SslOnConnect, ct);
                }
                else if (_settings.UseStartTls)
                {
                    await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
                }
                await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, ct);
                await smtp.SendAsync(mail, ct);
                await smtp.DisconnectAsync(true, ct);

                #endregion

                return true;
            }
            catch (Exception e)
            {
                var c = e;
                return false;
            }
        }

        public string GetEmailTemplate(string emailTemplate, TemplateData emailTemplateModel)
        {
            string mailTemplate = LoadTemplate(emailTemplate);

            IRazorEngine razorEngine = new RazorEngineCore.RazorEngine();
            IRazorEngineCompiledTemplate modifiedMailTemplate = razorEngine.Compile(mailTemplate);

            return modifiedMailTemplate.Run(emailTemplateModel);
        }

        public string LoadTemplate(string emailTemplate)
        {
            string baseDir = AppDomain.CurrentDomain.FriendlyName + "";
            string templateDir = Path.Combine(baseDir, "../../../../Files/EmailTemplates/");
            string templatePath = Path.Combine(templateDir, $"{emailTemplate}.cshtml");
            
            using FileStream fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader streamReader = new StreamReader(fileStream, Encoding.Default);

            string mailTemplate = streamReader.ReadToEnd();
            streamReader.Close();

            return mailTemplate;
        }
    }
}
