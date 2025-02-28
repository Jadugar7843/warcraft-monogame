﻿using Warcraft.Managers;
using Warcraft.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Warcraft.UI;
using System;

namespace Warcraft.Units
{
    enum WorkigState
    {
        NOTHING,
        WAITING_PLACE,
        GO_TO_WORK,
        WORKING
    }

    abstract class Unit
    {
        protected Dictionary<AnimationType, Texture2D> texture = new Dictionary<AnimationType, Texture2D>();
        public Animation animations;

        public Vector2 position;
        private Vector2 goal;

        public WorkigState workState = WorkigState.NOTHING;
        public bool selected;
        public bool transition;

        public Rectangle rectangle;

        protected int width;
        protected int height;
        protected int speed;

        public UI.UI ui;
        protected Dictionary<AnimationType, string> textureName = new Dictionary<AnimationType, string>();

        public InformationUnit information;
        public static InformationUnit Information;

        Pathfinding pathfinding;
        List<Util.PathNode> path;

        public Unit target;

        Vector2 lastPosition;

        Point oldAdjust;

        Vector2 targetPosition;
        Vector2 missilePosition;
        Texture2D missileTroll;
        Texture2D missileElven;

        bool shoot = false;
        float angle = 0;

        public Unit(int tileX, int tileY, int width, int height, int speed, ManagerMouse managerMouse, ManagerMap managerMap, ManagerBuildings managerBuildings)
        {
            this.width = width;
            this.height = height;
            this.speed = speed;
            
            pathfinding = new Pathfinding(managerMap);
            position = new Vector2(tileX * Warcraft.TILE_SIZE, tileY * Warcraft.TILE_SIZE);
            
            managerMouse.MouseEventHandler += ManagerMouse_MouseEventHandler;

            rectangle = new Rectangle((int)position.X, (int)position.Y, width, height);
        }

        private void ManagerMouse_MouseEventHandler(object sender, Events.MouseEventArgs e)
        {
            if (!e.UI && workState == WorkigState.NOTHING)
            {
                if (rectangle.Intersects(e.SelectRectangle) && information.HitPoints > 0)
                    selected = true;
                else
                    selected = false;
            }
        }

        public virtual void LoadContent(ContentManager content)
        {
            texture[AnimationType.WALKING] = content.Load<Texture2D>(textureName[AnimationType.WALKING]);

            missileTroll = content.Load<Texture2D>("axe");
            missileElven = content.Load<Texture2D>("arrow");

            ui.LoadContent(content);

            GeneticUtil.Encode(this);
        }

        public virtual void Update()
        {
            animations.Update();
            ui.Update();

            if (information.HitPoints > 0)
            {
                if (transition)
                {
                    UpdateTransition();

                    if (target != null)
                    {
                        int adjustX = ((int)target.position.X - (int)position.X) / 32;
                        int adjustY = ((int)target.position.Y - (int)position.Y) / 32;

                        if (Math.Abs(adjustX) > information.Range || Math.Abs(adjustY) > information.Range)
                        {
                            transition = false;
                        }
                    }
                }
                else if (animations.currentAnimation != AnimationType.DYING)
                    animations.Stop();
            }

            if (animations.currentAnimation != AnimationType.DYING && information.HitPoints <= 0)
            {
                selected = false;

                animations.currentAnimation = AnimationType.DYING;
                animations.isLooping = false;
                animations.Play("dying");

                if (information.Faction == Faction.ALLIANCE)
                    Warcraft.FOOD++;
            }

            if (information.HitPoints > 0)
                Combat();
        }

        public void Combat()
        {
            if (target != null)
            {
                int x = 0, y = 0;
                if (animations.current.ToLower().Contains("down"))
                    y = -1;
                if (animations.current.ToLower().Contains("up"))
                    y = 1;
                if (animations.current.ToLower().Contains("left"))
                    x = 1;
                if (animations.current.ToLower().Contains("right"))
                    x = -1;

                int adjustX = ((int)target.position.X - (int)position.X) / 32;
                int adjustY = ((int)target.position.Y - (int)position.Y) / 32;

                float distance = Vector2.Distance(target.position, position);

                if (adjustX == 5 && oldAdjust.X == 6 && adjustY == 6 && oldAdjust.Y == 5)
                {
                    adjustX = 4;
                    adjustY = 4;
                }

                if (!shoot)
                    missilePosition = position;

                if ((Math.Abs(adjustX) > information.Range || Math.Abs(adjustY) > information.Range) && lastPosition != position)
                {
                    Move((int)Math.Max(0, target.position.X / 32 + information.Range * x),
                         (int)Math.Max(0, target.position.Y / 32 + information.Range * y));
                    lastPosition = position;

                    animations.currentAnimation = AnimationType.WALKING;
                    animations.Play(animations.current);

                    if (information.Type == Util.Units.TROLL_AXETHROWER)
                    {
                        missilePosition = position;
                        targetPosition = target.position;
                        shoot = false;
                    }
                }
                else
                {
                    if (information.Type == Util.Units.TROLL_AXETHROWER || information.Type == Util.Units.ELVEN_ARCHER)
                    {
                        if (targetPosition == Vector2.Zero)
                            targetPosition = target.position;

                        transition = false;
                        shoot = true;

                        if (information.Type == Util.Units.TROLL_AXETHROWER)
                            angle += 0.1f;
                        else
                        {
                            double opposite = Math.Abs(position.Y - target.position.Y);
                            double adjacent = Math.Abs(position.X - target.position.X);

                            angle = (float)Math.Atan(opposite / adjacent);// (float)(Math.Atan2(position.X, -position.Y));
                        }

                        Vector2 difference = targetPosition - missilePosition;
                        difference.Normalize();

                        missilePosition += difference * 5f;

                        if ((int)Vector2.Distance(missilePosition, targetPosition) <= 2)
                        {
                            angle = 0;

                            missilePosition = position;
                            targetPosition = target.position;

                            float reduce = ((information.Damage * ((float)information.Precision / 100)) - target.information.Armor) / 30;
                            target.information.HitPoints -= reduce < 0 ? 0.01f : reduce;
                            information.Fitness += reduce < 0 ? 0.01f : reduce;
                        }
                    }
                    else
                    {
                        float reduce = ((information.Damage * ((float)information.Precision / 100)) - target.information.Armor) / 30;
                        target.information.HitPoints -= reduce < 0 ? 0.01f : reduce;
                        information.Fitness += reduce < 0 ? 0.01f : reduce;
                    }

                    if (adjustX > 0 && adjustY == 0) animations.Change("right");
                    else if (adjustX > 0 && adjustY > 0) animations.Change("downRight");
                    else if (adjustX == 0 && adjustY > 0) animations.Change("down");
                    else if (adjustX < 0 && adjustY > 0) animations.Change("downLeft");
                    else if (adjustX < 0 && adjustY == 0) animations.Change("left");
                    else if (adjustX < 0 && adjustY < 0) animations.Change("upLeft");
                    else if (adjustX == 0 && adjustY < 0) animations.Change("up");
                    else if (adjustX > 0 && adjustY < 0) animations.Change("upRight");

                    animations.currentAnimation = AnimationType.ATTACKING;
                    animations.Play(animations.current);
                }

                oldAdjust = new Point(adjustX, adjustY);

                if (Math.Abs(adjustX) > 4 + information.Range || Math.Abs(adjustY) > 4 + information.Range || 
                    target.information.HitPoints <= 0 || information.HitPoints <= 0 ||
                    target.workState != WorkigState.NOTHING)
                {
                    target = null;
                    shoot = false;

                    animations.currentAnimation = AnimationType.WALKING;
                    animations.Play(animations.current);
                }
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (information.HitPoints > 0)
            { 
                if (selected)
                    SelectRectangle.Draw(spriteBatch, new Rectangle(rectangle.X, rectangle.Y, 32, 32));

                if (shoot && animations.currentAnimation == AnimationType.ATTACKING)
                {
                    if (information.Type == Util.Units.TROLL_AXETHROWER)
                        spriteBatch.Draw(missileTroll, missilePosition + new Vector2(15, 15), new Rectangle(5, 5, 19, 20), Color.White, angle, new Vector2(9.5f, 10), 1f, SpriteEffects.None, 0);
                    else if (information.Type == Util.Units.ELVEN_ARCHER)
                        spriteBatch.Draw(missileElven, missilePosition + new Vector2(15, 15), new Rectangle(19, 4, 3, 30), Color.White, angle, new Vector2(1.5f, 15), 1f, SpriteEffects.None, 0);
                }
            }
            

            if (workState != WorkigState.WORKING)
            {
                if (animations.FlipX())
                    spriteBatch.Draw(texture[animations.currentAnimation], position, animations.rectangle, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally, 0);
                else if (animations.FlipY())
                    spriteBatch.Draw(texture[animations.currentAnimation], position, animations.rectangle, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipVertically, 0);
                else
                    spriteBatch.Draw(texture[animations.currentAnimation], position, animations.rectangle, Color.White);
            }
        }

        public virtual void DrawUI(SpriteBatch spriteBatch)
        {
            if (selected)
            {
                ui.Draw(spriteBatch);
            }
        }

        public void Move(int xTile, int yTile)
        {
            if (information.HitPoints > 0)
            {
                if (pathfinding.SetGoal((int)position.X, (int)position.Y, xTile, yTile))
                {
                    path = pathfinding.DiscoverPath();
                    if (path.Last().x == 49 && path.Last().y == 49)
                    {
                        // 
                    }
                    else
                    {
                        if (path.Count > 0)
                        {
                            transition = true;
                            goal = new Vector2(path.First().x * 32, path.First().y * 32);
                            path.RemoveAt(0);
                        }
                        else
                            position = new Vector2(xTile * 32, yTile * 32);
                    }
                }
            }
        }

        public void MoveTo(int xTile, int yTile)
        {
            if (information.HitPoints > 0)
            {
                if (pathfinding.SetGoal((int)position.X, (int)position.Y, (int)position.X / 32 + xTile, (int)position.Y / 32 + yTile))
                {
                    transition = true;

                    path = pathfinding.DiscoverPath();

                    goal = new Vector2(path.First().x * 32, path.First().y * 32);
                    path.RemoveAt(0);
                }
            }
        }

        public void UpdateTransition()
        {
            if (information.HitPoints > 0)
            {
                if (position.X < goal.X && position.Y < goal.Y)
                {
                    position.X += speed;
                    position.Y += speed;
                    animations.Play("downRight");
                }
                else if (position.X < goal.X && position.Y > goal.Y)
                {
                    position.X += speed;
                    position.Y -= speed;
                    animations.Play("upRight");
                }
                else if (position.X > goal.X && position.Y < goal.Y)
                {
                    position.X -= speed;
                    position.Y += speed;
                    animations.Play("downLeft");
                }
                else if (position.X > goal.X && position.Y > goal.Y)
                {
                    position.X -= speed;
                    position.Y -= speed;
                    animations.Play("upLeft");
                }
                else if (position.X < goal.X)
                {
                    position.X += speed;
                    animations.Play("right");
                }
                else if (position.X > goal.X)
                {
                    position.X -= speed;
                    animations.Play("left");
                }
                else if (position.Y < goal.Y)
                {
                    position.Y += speed;
                    animations.Play("down");
                }
                else if (position.Y > goal.Y)
                {
                    position.Y -= speed;
                    animations.Play("up");
                }

                if (position.X == goal.X && position.Y == goal.Y)
                {
                    if (path.Count > 0)
                    {
                        goal = new Vector2(path.First().x * 32, path.First().y * 32);
                        path.RemoveAt(0);
                    }
                    else
                    {
                        transition = false;
                        if (workState == WorkigState.GO_TO_WORK)
                        {
                            workState = WorkigState.WORKING;
                            selected = false;
                        }
                    }
                }

                rectangle.X = (int)position.X;
                rectangle.Y = (int)position.Y;
            }
        }
    }
}
