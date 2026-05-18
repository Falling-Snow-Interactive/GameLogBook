using GameLogBook.Models;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Pages;

public partial class Platforms : ComponentBase
{
    private bool isAddPopupOpen;
    
    private List<Platform> platforms;
}