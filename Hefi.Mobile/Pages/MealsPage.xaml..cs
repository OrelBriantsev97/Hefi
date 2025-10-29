using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hefi.Mobile.Services;
using Hefi.Mobile.Pages;

namespace Hefi.Mobile.Pages
{
    /// <summary>
    /// Meals listing page- displays the user's meals and navigates to AddMealPage to add a new meal.
/// </summary>
    public partial class MealsPage : ContentPage
    {
        private readonly MealsService _meals;
        private readonly AddMealPage _addMealPage;

        // initialize MealsPage with injected MealsService and AddMealPage
        public MealsPage(MealsService meals,AddMealPage addMealPage)
        {
            InitializeComponent();
            _meals = meals;
            _addMealPage = addMealPage;
        }

        // loads meals when the page becomes visible.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                MealsList.ItemsSource = await _meals.GetMealsAsync();
                await LoadMealsAsynce();
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("Network", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // navigates to the Add Meal page.
        private async void OnAddClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(_addMealPage);
        }

        // Fetches meals and binds them to the list control
        private async Task LoadMealsAsynce()
        {
            try
            {
                MealsList.ItemsSource = await _meals.GetMealsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

}
