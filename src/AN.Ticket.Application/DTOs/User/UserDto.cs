﻿using AN.Ticket.Domain.Enums;

namespace AN.Ticket.Application.DTOs.User;
public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string ProfilePicture { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
