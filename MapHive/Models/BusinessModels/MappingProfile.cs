namespace MapHive.Models.BusinessModels
{
    using AutoMapper;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Repository -> ViewModel
            _ = CreateMap<MapLocationGet, MapLocationViewModel>();
            _ = CreateMap<ReviewGet, ReviewViewModel>();
            _ = CreateMap<DiscussionThreadGet, ThreadDetailsViewModel>();
            _ = CreateMap<ThreadMessageGet, ThreadMessageViewModel>();

            // ViewModel -> Repository Create/Update DTOs
            _ = CreateMap<ReviewViewModel, ReviewCreate>();
            _ = CreateMap<ReviewViewModel, ReviewUpdate>();
            _ = CreateMap<DiscussionThreadViewModel, DiscussionThreadCreate>();
            _ = CreateMap<ThreadMessageViewModel, ThreadMessageCreate>();
            // Category mapping
            _ = CreateMap<CategoryGet, CategoryUpdate>();

            _ = CreateMap<UserGet, UserLogin>();
            _ = CreateMap<UserLogin, UserUpdate>();
            _ = CreateMap<UserGet, UserUpdate>();
            _ = CreateMap<UserGet, UserLogin>();

            _ = CreateMap<DataGridGet, DataGridViewModel>();
        }
    }
}