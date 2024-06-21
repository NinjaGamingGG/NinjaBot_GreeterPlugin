using ImageMagick;


namespace GreeterPlugin.PluginHelpers;

public static class GenerateWelcomeImage
{
    public static async Task Generator(string username, string avatarUrl, string welcomeText, int memberCount,
        string backgroundUrl, bool roundedAvatar, double offsetX, double offsetY, string welcomeCardPath,
        int cornerRadius ,bool whiteCorner = true)
    {
        var baseImage = new MagickImage(Path.Combine(GreeterPlugin.StaticPluginDirectory,backgroundUrl));
        
        var avatarImage = new MagickImage(await GetUserAvatar(avatarUrl));
        
        avatarImage.Resize(640,640);

        var avatarBorder = new MagickImage(MagickColors.White, avatarImage.Width+10, avatarImage.Height+10);
        
        if (roundedAvatar)
        {
            avatarImage = UserAvatarRoundedCorners(avatarImage, cornerRadius);
            avatarBorder = UserAvatarRoundedCorners(avatarBorder, cornerRadius);
        }

        
        var drawableAvatar =
            new DrawableComposite(offsetX, offsetY, CompositeOperator.Over , avatarImage);
        var drawableBorder = new DrawableComposite(offsetX-5,offsetY-5, CompositeOperator.Over, avatarBorder);
        
        var drawableText = new DrawableText(900, 400, welcomeText.Replace("{username}",username));
        var drawableSubText = new DrawableText(1000, 650, $"Member: #{memberCount}");
        
        var drawableSubTextFontPointSize = new DrawableFontPointSize(30);
        var drawableFillColor = new DrawableFillColor(MagickColors.White);
        var drawableSubTextFont = new DrawableFont("Cantarell");
        
        if (whiteCorner)
            baseImage.Draw(drawableBorder);
        
        baseImage.Draw(drawableAvatar);
        
        
        var readSettings = new MagickReadSettings
        {
            FillColor = MagickColors.White,
            BackgroundColor = MagickColors.Transparent,
            TextGravity = Gravity.South,
            // This determines the size of the area where the text will be drawn in
            Width = 600,
            Height = 350,
            FontPointsize = 50
        };

        if (welcomeText.Contains("{username}"))
            welcomeText = welcomeText.Replace("{username}", username);
        
        using (var label = new MagickImage($"caption:{welcomeText}", readSettings))
        {
            //baseImage.Composite(label, 900, 200, CompositeOperator.Over);
            var drawableBoundingText =
                new DrawableComposite(900, 200, CompositeOperator.Over , label);
            baseImage.Draw(drawableBoundingText);
        }
        
        
        
        baseImage.Draw(drawableSubText,drawableFillColor,drawableSubTextFontPointSize,drawableSubTextFont);
        
        await baseImage.WriteAsync(welcomeCardPath);
        baseImage.Dispose();


    }

    private static async Task<MagickImage> GetUserAvatar(string avatarUrl)
    {
        var httpClient = new HttpClient();

        var res = await httpClient.GetAsync(avatarUrl);

        var bytes = await res.Content.ReadAsByteArrayAsync();

        return new MagickImage(bytes);
    }

    private static  MagickImage UserAvatarRoundedCorners(MagickImage userAvatar, int cornerRadius)
    {
        var image = new MagickImage(userAvatar);

        using var mask = new MagickImage(MagickColors.White, image.Width, image.Height);
        
        new Drawables()
            .FillColor(MagickColors.Black)
            .StrokeColor(MagickColors.Black)
            .Polygon(new PointD(0, 0), new PointD(0, cornerRadius), new PointD(cornerRadius, 0))
            .Polygon(new PointD(mask.Width, 0), new PointD(mask.Width, cornerRadius), new PointD(mask.Width - cornerRadius, 0))
            .Polygon(new PointD(0, mask.Height), new PointD(0, mask.Height - cornerRadius), new PointD(cornerRadius, mask.Height))
            .Polygon(new PointD(mask.Width, mask.Height), new PointD(mask.Width, mask.Height - cornerRadius), new PointD(mask.Width - cornerRadius, mask.Height))
            .FillColor(MagickColors.White)
            .StrokeColor(MagickColors.White)
            .Circle(cornerRadius, cornerRadius, cornerRadius, 0)
            .Circle(mask.Width - cornerRadius, cornerRadius, mask.Width - cornerRadius, 0)
            .Circle(cornerRadius, mask.Height - cornerRadius, 0, mask.Height - cornerRadius)
            .Circle(mask.Width - cornerRadius, mask.Height - cornerRadius, mask.Width - cornerRadius, mask.Height)
            .Draw(mask);

        // This copies the pixels that were already transparent on the mask.
        using (var imageAlpha = image.Clone())
        {
            imageAlpha.Alpha(AlphaOption.Extract);
            imageAlpha.Opaque(MagickColors.White, MagickColors.None);
            mask.Composite(imageAlpha, CompositeOperator.Over);
        }

        mask.HasAlpha = false;
        image.HasAlpha = false;
        image.Composite(mask, CompositeOperator.CopyAlpha);
        return image;

    }
    
    
    



}