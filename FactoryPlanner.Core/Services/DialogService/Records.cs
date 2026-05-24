using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Services.Interfaces
{
    public record DialogOptions(
        MessageType Type, string Title, string Message,
        string PrimaryText, string SecondaryText, string CloseText,
        ContentDialogButton DefaultButton = ContentDialogButton.Primary
    );
}
