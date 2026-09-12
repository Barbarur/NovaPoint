namespace NovaPointViewModels
{
    public interface INavigationService
    {
        void NavigateTo(object view);
        void GoBack();
    }
}
