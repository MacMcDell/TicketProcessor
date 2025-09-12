using AutoMapper;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;

namespace TicketProcessor.Infrastructure.Mapping;

public class EntityToDtoProfile : Profile
{
    public EntityToDtoProfile()
    {
        CreateMap<Venue, VenueDto>();
        CreateMap<Event, EventDto>();
        CreateMap<EventTicketType, EventTicketTypeDto>();
        CreateMap<Reservation, ReservationDto>();

// // Reverse maps if you plan to accept DTOs on input
//         CreateMap<VenueDto, Venue>();
//         CreateMap<EventDto, Event>();
//         CreateMap<TicketTypeDto, TicketType>();
//         CreateMap<EventTicketTypeDto, EventTicketType>();
//         CreateMap<ReservationDto, Reservation>();

        CreateMap<VenueDto, Venue>()
            .ForMember(d => d.Created, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.LastModified, o => o.Ignore())
            .ForMember(d => d.LastModifiedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());

        CreateMap<EventDto, Event>()
            .ForMember(d => d.Venue, o => o.Ignore())
            .ForMember(d => d.TicketTypes, o => o.Ignore())
            .ForMember(d => d.Created, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.LastModified, o => o.Ignore())
            .ForMember(d => d.LastModifiedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());

       CreateMap<EventTicketTypeDto, EventTicketType>()
            .ForMember(d => d.Event, o => o.Ignore())
            .ForMember(d => d.Created, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.LastModified, o => o.Ignore())
            .ForMember(d => d.LastModifiedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());

        CreateMap<ReservationDto, Reservation>()
            .ForMember(d => d.EventTicketType, o => o.Ignore())
            .ForMember(d => d.Created, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.LastModified, o => o.Ignore())
            .ForMember(d => d.LastModifiedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());
    }
}