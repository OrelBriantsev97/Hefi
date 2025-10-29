using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hefi.Mobile.Services;

namespace Hefi.Mobile.Pages
{
    // Code-behind for the Add workout page.
    // Connects the XAML view to its AddWorkoutViewModel via dependency injection,
    public partial class AddWorkoutPage : ContentPage
    {
        private readonly WorkoutsService _svc;

        public AddWorkoutPage(WorkoutsService svc)
        {
            InitializeComponent();
            _svc = svc;
        }

        // Handles the save button click: validates input, sends the workout data, and provides user feedback.
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var dto = new WorkoutCreate(
                    TypeEntry.Text?.Trim() ?? "",
                    int.TryParse(DurationEntry.Text, out var d) ? d : 0,
                    int.TryParse(KcalEntry.Text, out var k) ? k : 0,
                    null
                );
                var id = await _svc.AddAsync(dto);
                await DisplayAlert("Saved", $"Workout #{id} added.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
            }
        }
    }
}

