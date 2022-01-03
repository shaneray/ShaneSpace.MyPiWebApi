using AutoMapper;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    public abstract class BaseViewModel<TSource> : IViewModel
    {
        public void RegisterMapping(Profile mappingProfile)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var mappingExpression = mappingProfile.CreateMap(typeof(TSource), GetType()) as IMappingExpression<TSource, object>;
            ConfigureMapping(mappingExpression);
        }

        protected virtual void ConfigureMapping(IMappingExpression<TSource, object> mappingExpression)
        {
            // override to do custom mapping
        }
    }
}