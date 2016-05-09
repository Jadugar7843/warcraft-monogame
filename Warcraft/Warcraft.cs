﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Warcraft.Managers;
using Warcraft.UI;
using Warcraft.Util;

namespace Warcraft
{
    public class Warcraft : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        ManagerMap managerMap = new ManagerMap();
        ManagerMouse managerMouse = new ManagerMouse();

        ManagerUI managerUI;
        ManagerUnits managerUnits;
        ManagerBuildings managerBuildings;

        public static int WINDOWS_WIDTH = 960;
        public static int WINDOWS_HEIGHT = 608;

        public static int TILE_SIZE = 32;
        public static int MAP_SIZE = 50;

        public static Camera camera;

        public static int GOLD = 5000;
        public static int WOOD = 99999;
        public static int FOOD = 5;
        public static int OIL = 99999;

        public Warcraft()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = WINDOWS_WIDTH + 200;
            graphics.PreferredBackBufferHeight = WINDOWS_HEIGHT;

            Content.RootDirectory = "Content";

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            managerBuildings = new ManagerBuildings(managerMouse, managerMap);
            managerUnits = new ManagerUnits(managerMouse, managerMap, managerBuildings);
            managerUI = new ManagerUI(managerMouse, managerBuildings , managerUnits);

            camera = new Camera(GraphicsDevice.Viewport);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            
            managerMap.LoadContent(Content);
            managerUnits.LoadContent(Content);
            managerBuildings.LoadContent(Content);
            managerUI.LoadContent(Content);
            SelectRectangle.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            
            managerMouse.Update();
            managerUnits.Update();
            managerBuildings.Update();
            managerUI.Update();

            camera.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.transform);
            managerMap.Draw(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin();
            managerUI.DrawBack(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.transform);
            managerMouse.Draw(spriteBatch);
            managerUnits.Draw(spriteBatch);
            managerBuildings.Draw(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin();
            managerUI.Draw(spriteBatch);
            managerUnits.DrawUI(spriteBatch);
            managerBuildings.DrawUI(spriteBatch);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
