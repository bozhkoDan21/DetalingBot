using AutoMapper;

namespace DetalingBot.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Appointment
            CreateMap<Appointment, DTO_Appointment.Response>()
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.AppointmentDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<DTO_Appointment.Request, Appointment>()
                .ForMember(dest => dest.AppointmentDate, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.Time))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.Time.Add(TimeSpan.FromMinutes(60)))) 
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => AppointmentStatus.Confirmed))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // Review
            CreateMap<DTO_CreateReviewRequest, DTO_CreateReview>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) 
                .ForMember(dest => dest.PhotoBeforeTempId, opt => opt.Ignore())
                .ForMember(dest => dest.PhotoAfterTempId, opt => opt.Ignore());

            CreateMap<DTO_CreateReview, Review>()
                .ForMember(dest => dest.ReviewDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Review, DTO_Review>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.PhotoBeforeUrl, opt => opt.Ignore()) 
                .ForMember(dest => dest.PhotoAfterUrl, opt => opt.Ignore());

            // Service
            CreateMap<Service, DTO_Service>();
            CreateMap<ServiceCategory, DTO_ServiceCategory>().ReverseMap();
        }
    }
}
