using AutoMapper;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;

namespace MapHive.Models.BusinessModels
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Repository -> ViewModel
            _ = this.CreateMap<MapLocationGet, MapLocationViewModel>();
            _ = this.CreateMap<ReviewGet, ReviewViewModel>();
            _ = this.CreateMap<DiscussionThreadGet, ThreadDetailsViewModel>();
            _ = this.CreateMap<ThreadMessageGet, ThreadMessageViewModel>();

            // ViewModel -> Repository Create/Update DTOs
            _ = this.CreateMap<ReviewViewModel, ReviewCreate>();
            _ = this.CreateMap<ReviewViewModel, ReviewUpdate>();
            _ = this.CreateMap<DiscussionThreadViewModel, DiscussionThreadCreate>();
            _ = this.CreateMap<ThreadMessageViewModel, ThreadMessageCreate>();
            // Category mapping
            _ = this.CreateMap<CategoryGet, CategoryUpdate>();

            _ = this.CreateMap<UserGet, UserLogin>();
            _ = this.CreateMap<UserLogin, UserUpdate>();
            _ = this.CreateMap<UserGet, UserUpdate>();
            _ = this.CreateMap<UserGet, UserLogin>();

            _ = this.CreateMap<DataGridGet, DataGridViewModel>();
        }
    }
}