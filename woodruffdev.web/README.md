# woodruff.dev — ASP.NET Core Razor Pages

A redesigned personal website for Chris "Woody" Woodruff — Fractional Architect, Strategic Technology Advisor & Expert Witness.

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Locally

```bash
dotnet restore
dotnet run
```

The site will be available at `https://localhost:5001` (or `http://localhost:5000`).

## Project Structure

```
WoodruffDev/
├── Pages/
│   ├── Shared/
│   │   └── _Layout.cshtml          # Main layout with header, footer, nav
│   ├── Services/
│   │   ├── Index.cshtml             # Services overview
│   │   ├── FractionalArchitect.cshtml
│   │   ├── ExpertWitness.cshtml
│   │   ├── MicroConsulting.cshtml
│   │   ├── ProjectBased.cshtml
│   │   ├── Retainer.cshtml
│   │   └── Advisory.cshtml
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Index.cshtml                 # Homepage
│   ├── About.cshtml
│   ├── Blog.cshtml
│   ├── Contact.cshtml
│   └── Error.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css                 # Full design system
│   ├── js/
│   │   └── site.js                  # Interactions & animations
│   ├── images/
│   │   └── README.md                # Image placement guide
│   └── favicon.svg
├── Program.cs
├── WoodruffDev.csproj
└── appsettings.json
```

## Design Notes

- **Typography**: Instrument Serif (display) + DM Sans (body) via Google Fonts
- **Palette**: Warm neutrals with gold accent (#c9a96e)
- **Layout**: Editorial-refined, generous whitespace, scroll-animated sections
- **Responsive**: Fully responsive from mobile to desktop
- **Graceful fallbacks**: All images have inline fallbacks (emoji icons) if missing

## Adding Images

See `wwwroot/images/README.md` for the complete list of images to add.
All pages work with or without images — styled placeholders appear automatically.

## Pages

| Route | Description |
|-------|-------------|
| `/` | Homepage — Hero, projects, benefits, services, process, blog, testimonials, CTA |
| `/About` | Bio, mission/vision/goal tabs, contact details |
| `/Blog` | Blog listing with post previews (links to existing WordPress blog) |
| `/Contact` | Contact form with info sidebar |
| `/Services` | Services grid overview |
| `/Services/FractionalArchitect` | Fractional Architect detail |
| `/Services/ExpertWitness` | Expert Witness detail |
| `/Services/MicroConsulting` | Micro-Consulting detail |
| `/Services/ProjectBased` | Project-Based Contracts detail |
| `/Services/Retainer` | Retainer Services detail |
| `/Services/Advisory` | Advisory & Board Roles detail |
