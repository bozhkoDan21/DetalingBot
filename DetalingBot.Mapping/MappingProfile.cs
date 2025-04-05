using AutoMapper;

namespace DetalingBot.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Маппинг из Appointment в DTO
            CreateMap<Appointment, DTO_Appointment.Response>()
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.AppointmentDate))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.DurationMinutes,
                    opt => opt.MapFrom(src => (src.EndTime - src.StartTime).TotalMinutes));

            // Маппинг из DTO в Appointment
            CreateMap<DTO_Appointment.Request, Appointment>()
                .ForMember(dest => dest.AppointmentDate, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.Time))
                .ForMember(dest => dest.EndTime,opt => opt.MapFrom(src => src.Time.Add(TimeSpan.FromMinutes(src.DurationMinutes))))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => AppointmentStatus.Confirmed))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UserId,opt => opt.MapFrom((src, dest, _, context) => context.Items["CurrentUserId"]));

            CreateMap<DTO_Appointment.RescheduleRequest, Appointment>()
                .ForMember(dest => dest.AppointmentDate, opt => opt.MapFrom(src => src.NewDate))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.NewTime))
                .ForMember(dest => dest.EndTime, opt => opt.Ignore()) 
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
    }
}
