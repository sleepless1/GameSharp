using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine;
using Engine.Controls;
using SharpDX;

namespace GUI_Test
{
    class program : GameApplication
    {
        protected override void Initialize()
        {
            base.Initialize();

            var notice = new Notification("Aligned top-center");
            notice.Position = new Vector2(600f, 599f);
            notice.VerticalAlignment = VerticalAlignment.Top;
            notice.HorizontalAlignment = HorizontalAlignment.Center;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned bottom-center");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Bottom;
            notice.HorizontalAlignment = HorizontalAlignment.Center;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned left-center");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Center;
            notice.HorizontalAlignment = HorizontalAlignment.Left;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned right-center");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Center;
            notice.HorizontalAlignment = HorizontalAlignment.Right;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned center");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Center;
            notice.HorizontalAlignment = HorizontalAlignment.Center;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned top-left");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Top;
            notice.HorizontalAlignment = HorizontalAlignment.Left;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned top-right");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Top;
            notice.HorizontalAlignment = HorizontalAlignment.Right;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned bottom-left");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Bottom;
            notice.HorizontalAlignment = HorizontalAlignment.Left;
            ControlManager.AddControl(notice);

            notice = new Notification("Aligned bottom-right");
            notice.Position = new Vector2(600f, 400f);
            notice.VerticalAlignment = VerticalAlignment.Bottom;
            notice.HorizontalAlignment = HorizontalAlignment.Right;
            ControlManager.AddControl(notice);

            notice = new Notification("Image!");
            notice.Position = new Vector2(256f, 64f);
            notice.Size = new Vector2(512f, 512f);
            notice.ActiveTexturePath = "warning.png";
            notice.InactiveTexturePath = "warning.png";
            ControlManager.AddControl(notice);

            var window = new Window("Basic window");
            window.Position = new Vector2(500f, 250f);
            ControlManager.AddControl(window);
        }

        static void Main()
        {
            Engine.Assets.AssetManager.RootDirectory = "Content";
            using(var game = new program()) {
                game.Run();
            }
        }
    }
}
