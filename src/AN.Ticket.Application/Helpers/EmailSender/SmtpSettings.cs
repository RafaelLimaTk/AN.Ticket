﻿namespace AN.Ticket.Application.Helpers.EmailSender;
public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string ImapHost { get; set; }
    public int ImapPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
