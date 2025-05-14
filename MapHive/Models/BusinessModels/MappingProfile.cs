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
            // CategoryName mapping
            _ = CreateMap<CategoryGet, CategoryUpdate>();

            _ = CreateMap<AccountGet, UserLogin>();
            _ = CreateMap<UserLogin, UserUpdate>();
            _ = CreateMap<AccountGet, UserUpdate>();
            _ = CreateMap<AccountGet, UserLogin>();

            _ = CreateMap<DataGridGet, DataGridViewModel>();
            _ = CreateMap<MapLocationGet, MapLocationUpdate>();
        }
    }
}
