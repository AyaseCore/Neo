﻿using System;
using SharpDX;
using WoWEditor6.Scene;
using WoWEditor6.UI;
using WoWEditor6.Utils;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using WintabDN;
using System.Windows;

namespace WoWEditor6.Editing
{
    class EditManager
    {
        public static EditManager Instance { get; private set; }

        private DateTime mLastChange = DateTime.Now;

        private Point mLastCursorPosition = Cursor.Position;

        private float mInnerRadius = 18.0f;
        private float mOuterRadius = 20.0f;
        private float mIntensity = 32.0f;
        private float mAmount = 32.0f;
        private float mOpacity = 255.0f;
        private float mPenSensivity;
        private float mAmplitude;
        private bool mIsTabletOn = false;
        private bool mIsTablet_RChange = false;

        public float InnerRadius
        {
            get { return mInnerRadius; }
            set { HandleInnerRadiusChanged(value); }
        }

        public float OuterRadius
        {
            get { return mOuterRadius; }
            set { HandleOuterRadiusChanged(value); }
        }

        public float Intensity
        {
            get { return mIntensity; }
            set { HandleIntensityChanged(value); }
        }

        public float Amount
        {
            get { return mAmount; }
            set { HandleAmountChanged(value); }
        }

        public float Opacity
        {
            get { return mOpacity; }
            set { HandleOpacityChanged(value); }

        }

        public float PenSensivity
        {
            get { return mPenSensivity; }
            set { HandlePenSensivityChanged(value);  }
        }

        public bool IsTabletOn
        {
            get { return mIsTabletOn; }
            set { HandleTabletControlChanged(value);  }
        }
        
        public bool IsTablet_RChange
        {
            get { return mIsTablet_RChange; }
            set { HandleTabletRadiusChanged(value); }
        }

        public float Amplitude
        {
            get { return mAmplitude;  }
            set { HandleAllowedAmplitudeChanged(value); }
        }

        public bool IsTexturing { get { return (CurrentMode & EditMode.Texturing) != 0; } }

        public Vector3 MousePosition { get; set; }
        public bool IsTerrainHovered { get; set; }

        public EditMode CurrentMode { get; private set; }

        static EditManager()
        {
            Instance = new EditManager();
        }


        public void UpdateChanges()
        {
            ModelSpawnManager.Instance.OnUpdate();
            ModelEditManager.Instance.Update();

            var diff = DateTime.Now - mLastChange;
            if (diff.TotalMilliseconds < (IsTexturing ? 40 : 20))
                return;

            mLastChange = DateTime.Now;
            if ((CurrentMode & EditMode.Sculpting) != 0)
                TerrainChangeManager.Instance.OnChange(diff);
            else if ((CurrentMode & EditMode.Texturing) != 0)
                TextureChangeManager.Instance.OnChange(diff);

            var keyState = new byte[256];
            UnsafeNativeMethods.GetKeyboardState(keyState);
            var altDown = KeyHelper.IsKeyDown(keyState, Keys.Menu);
            var LMBDown = KeyHelper.IsKeyDown(keyState, Keys.LButton);
            var RMBDown = KeyHelper.IsKeyDown(keyState, Keys.RButton);
            var spaceDown = KeyHelper.IsKeyDown(keyState, Keys.Space);
            var MMBDown = KeyHelper.IsKeyDown(keyState, Keys.MButton);
            var tDown = KeyHelper.IsKeyDown(keyState, Keys.T);

            var curPos = Cursor.Position;
            var amount = -(mLastCursorPosition.X - curPos.X) / 32.0f;

            if (mIsTabletOn) // All tablet control editing is here.
            {
                if (EditorWindowController.Instance.TexturingModel != null)
                {
                    mAmount = TabletManager.Instance.TabletPressure * mPenSensivity;
                    HandleAmountChanged(mAmount);
                }

                if (EditorWindowController.Instance.TerrainManager != null)
                {
                    mIntensity = TabletManager.Instance.TabletPressure * mPenSensivity;
                    HandleIntensityChanged(mIntensity);
                }

                if (mIsTablet_RChange) // If outer radius change is enabled.
                {
                    if (EditorWindowController.Instance.TexturingModel != null)
                    {
                        //float proportion = mInnerRadius / mOuterRadius; 

                        mOuterRadius = TabletManager.Instance.TabletPressure * mPenSensivity;
                        mInnerRadius = mOuterRadius * 0.5f; // Ugly way of doing that. If someone has an idea how to handle it properly, please implement.

                        if(mOuterRadius < 0.1f)
                        {
                            mOuterRadius = 0.1f;
                        }

                        if(mOuterRadius > mAmplitude)
                        {
                            mOuterRadius = mAmplitude;
                        }

                        if(mInnerRadius > mAmplitude)   
                        {
                            mInnerRadius = mAmplitude;
                        }
                        
                        if(mInnerRadius > mOuterRadius)
                        {
                            mInnerRadius = mOuterRadius;
                        }

                        HandleOuterRadiusChanged(mOuterRadius);
                        HandleInnerRadiusChanged(mInnerRadius);
                    }
                }
            }

            
            /*if (!LMBDown && IsTabletOn) // When tablet mode is on we always set those to minimal value (NEEDS TO BE MOVED OUT OF HERE).
            {
                mAmount = 1.0f;
                mIntensity = 1.0f;
            }*/
                 

            if (curPos != mLastCursorPosition)
            { 
                if (altDown && RMBDown)
                {
                    mInnerRadius += amount;


                    if (mInnerRadius < 0)
                    {
                        mInnerRadius = 0.0f;
                    }

                    if (mInnerRadius > 200)
                    {
                        mInnerRadius = 200.0f;
                    }

                    if (mInnerRadius > mOuterRadius)
                    {
                        mInnerRadius = mOuterRadius;
                    }

                    HandleInnerRadiusChanged(mInnerRadius);

                }
                

                if (altDown && LMBDown)
                {
                    mInnerRadius += amount;
                    mOuterRadius += amount;

                    if (mInnerRadius < 0)
                    {
                        mInnerRadius = 0.0f;
                    }

                    if (mInnerRadius > 200)
                    {
                        mInnerRadius = 200.0f;
                    }

                    if (mOuterRadius < 0)
                    {
                        mOuterRadius = 0.0f;
                    }

                    if (mOuterRadius > 200)
                    {
                        mOuterRadius = 200.0f;
                    }

                    if (mInnerRadius > mOuterRadius)
                    {
                        mInnerRadius = mOuterRadius;
                    }

                    HandleInnerRadiusChanged(mInnerRadius);
                    HandleOuterRadiusChanged(mOuterRadius);
                    

                }

                if(spaceDown && LMBDown)
                {
                    mIntensity += amount;
                    mAmount += amount;

                    if (EditorWindowController.Instance.TerrainManager != null)
                    {
                        if (mIntensity < 1)
                        {
                            mIntensity = 1.0f;
                        }

                        if (mIntensity > 40)
                        {
                            mIntensity = 40.0f;
                        }

                        HandleIntensityChanged(mIntensity);
                    }

                    if (EditorWindowController.Instance.TexturingModel != null)
                    {
                        if(mAmount < 1)
                        {
                            mAmount = 1.0f;
                        }

                        if (mAmount > 40)
                        {
                            mAmount = 40.0f;
                        }

                        HandleAmountChanged(mAmount);
                    }
                                     
                }

                if(altDown && MMBDown)
                {
                    mOpacity += amount;

                    if (EditorWindowController.Instance.TexturingModel != null)
                    {
                        if (mOpacity < 0)
                        {
                            mOpacity = 0.0f;
                        }

                        if (mOpacity > 255)
                        {
                            mOpacity = 255.0f;
                        }

                        HandleOpacityChanged(mOpacity);
                    }
                }

                if(spaceDown && MMBDown)
                {
                    mPenSensivity += amount / 32.0f;

                    if(EditorWindowController.Instance.TexturingModel != null)
                    {
                        if (mPenSensivity < 0.1f)
                        {
                            mPenSensivity = 0.1f;
                        }

                        if (mPenSensivity > 1.0f)
                        {
                            mPenSensivity = 1.0f;
                        }

                        HandlePenSensivityChanged(mPenSensivity);
                    }
                }

                if (spaceDown && tDown) // DOES NOT WORK PROPERLY. NEEDS TO BE MOVED OUT OF THIS METHOD.
                {
                    if (mIsTabletOn)
                    {
                        mIsTabletOn = false;
                    }
                    else
                    {
                        mIsTabletOn = true;
                    }
                    HandleTabletControlChanged(mIsTabletOn);
                }

                mLastCursorPosition = Cursor.Position;

            }


        }

        public void EnableSculpting()
        {
            CurrentMode |= EditMode.Sculpting;
            CurrentMode &= ~EditMode.Texturing;
        }

        public void DisableSculpting()
        {
            CurrentMode &= ~EditMode.Sculpting;
        }

        public void EnableTexturing()
        {
            CurrentMode |= EditMode.Texturing;
            CurrentMode &= ~EditMode.Sculpting;
        }

        public void DisableTexturing()
        {
            CurrentMode &= ~EditMode.Texturing;
        }

        private void HandleInnerRadiusChanged(float value)
        {
            mInnerRadius = value;
            WorldFrame.Instance.UpdateBrush(mInnerRadius, mOuterRadius);
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleInnerRadiusChanged(value);

            if (EditorWindowController.Instance.TerrainManager != null)
                EditorWindowController.Instance.TerrainManager.HandleInnerRadiusChanged(value);

        }

        private void HandleOuterRadiusChanged(float value)
        {
            mOuterRadius = value;
            WorldFrame.Instance.UpdateBrush(mInnerRadius, mOuterRadius);
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleOuterRadiusChanged(value);

            if (EditorWindowController.Instance.TerrainManager != null)
                EditorWindowController.Instance.TerrainManager.HandleOuterRadiusChanged(value);
        }

        private void HandleIntensityChanged(float value)
        {
            mIntensity = value;
            if (EditorWindowController.Instance.TerrainManager != null)
                EditorWindowController.Instance.TerrainManager.HandleIntensityChanged(value);

        }

        private void HandleAmountChanged(float value)
        {
            mAmount = value;
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleAmoutChanged(value);
        }

        private void HandleOpacityChanged(float value)
        {
            mOpacity = value;
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleOpacityChanged(value);
        }

        private void HandlePenSensivityChanged(float value)
        {
            mPenSensivity = value;
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandlePenSensivityChanged(value);
            if (EditorWindowController.Instance.TerrainManager != null)
                EditorWindowController.Instance.TerrainManager.HandlePenSensivityChanged(value);
        }

        private void HandleTabletControlChanged(bool value)
        {
            mIsTabletOn = value;
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleTabletControlChanged(value);
            if (EditorWindowController.Instance.TerrainManager != null)
                EditorWindowController.Instance.TerrainManager.HandleTabletControlChanged(value);
        }

        private void HandleTabletRadiusChanged(bool value)
        {
            mIsTablet_RChange = value;
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleTabletChangeRadiusChanged(value);
        }
        
        private void HandleAllowedAmplitudeChanged(float value)
        {
            mAmplitude = value;
            if (EditorWindowController.Instance.TexturingModel != null)
                EditorWindowController.Instance.TexturingModel.HandleAllowedAmplitudeChanged(value);
        }
    }
}
