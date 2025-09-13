using AutoMapper;
using TicketProcessor.Domain;

namespace TicketProcessor.Infrastructure.Mapping;

public class EntityToDtoProfile : Profile
{
    public EntityToDtoProfile()
    {
        CreateMap<Venue, VenueDto>();
        CreateMap<Event, EventDto>();
        CreateMap<EventTicketType, EventTicketTypeDto>();
        CreateMap<Reservation, ReservationResponseDto>();

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

        CreateMap<ReservationResponseDto, Reservation>()
            .ForMember(d => d.EventTicketType, o => o.Ignore())
            .ForMember(d => d.Created, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.LastModified, o => o.Ignore())
            .ForMember(d => d.LastModifiedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());
    }
}