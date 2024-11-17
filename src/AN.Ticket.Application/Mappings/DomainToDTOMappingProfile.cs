using AN.Ticket.Application.DTOs.Activity;
using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.DTOs.Attachment;
using AN.Ticket.Application.DTOs.InteractionHistory;
using AN.Ticket.Application.DTOs.SatisfactionRating;
using AN.Ticket.Application.DTOs.Team;
using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Domain.Entities;
using AutoMapper;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Mappings;
public class DomainToDTOMappingProfile
    : Profile
{
    public DomainToDTOMappingProfile()
    {
        CreateMap<DomainEntity.Ticket, TicketDto>();
        CreateMap<TicketMessage, TicketMessageDto>();
        CreateMap<Activity, ActivityDto>();
        CreateMap<InteractionHistory, InteractionHistoryDto>();
        CreateMap<SatisfactionRating, SatisfactionRatingDto>();
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<Attachment, AttachmentDto>();
        CreateMap<Team, TeamDto>().ReverseMap();
        CreateMap<Asset, AssetDto>().ReverseMap();
    }
}
