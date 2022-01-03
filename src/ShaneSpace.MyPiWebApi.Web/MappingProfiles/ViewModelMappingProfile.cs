using AutoMapper;
using ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi;
using System;
using System.Linq;

namespace ShaneSpace.MyPiWebApi.Web.MappingProfiles
{
    /// <summary>
    /// View Model Mapping Profile
    /// </summary>
    public class ViewModelMappingProfile : Profile
    {
        /// <inheritdoc />
        public ViewModelMappingProfile()
        {
            var viewModels = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IViewModel).IsAssignableFrom(p) && !p.IsAbstract)
                .Select(x => (IViewModel)Activator.CreateInstance(x));
            foreach (var viewModel in viewModels)
            {
                viewModel.RegisterMapping(this);
            }
        }
    }
}
