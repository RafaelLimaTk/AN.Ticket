﻿namespace AN.Ticket.Application.Interfaces;
public interface IEmailSenderService
{
    Task SendEmailAsync(string email, string subject, string message);
}
