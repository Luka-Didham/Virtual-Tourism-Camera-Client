/* 
 * Copyright (C) 2021 because-why-not.com Limited
 * 
 * Please refer to the license.txt for license information
 */

using System.Text;
using UnityEngine;

namespace Byn.Awrtc.Browser
{
    public class BrowserMediaNetwork : BrowserWebRtcNetwork, IMediaNetwork
    {

        private FramePixelFormat mFormat = FramePixelFormat.ABGR;

        public BrowserMediaNetwork(NetworkConfig lNetConfig)
        {

            string conf = CAPI.NetworkConfigToJson(lNetConfig);

            SLog.L("Creating BrowserMediaNetwork config: " + conf, this.GetType().Name);
            mReference = CAPI.Unity_MediaNetwork_Create(conf);
        }


        private void SetOptional(int? opt, ref int value)
        {
            if (opt.HasValue)
            {
                value = opt.Value;
            }
        }
        public void Configure(MediaConfig config)
        {
            mFormat = config.Format;
            int minWidth = -1;
            int minHeight = -1;
            int maxWidth = -1;
            int maxHeight = -1;
            int idealWidth = -1;
            int idealHeight = -1;
            int minFrameRate = -1;
            int maxFrameRate = -1;
            int idealFrameRate = -1;

            SetOptional(config.MinWidth, ref minWidth);
            SetOptional(config.MinHeight, ref minHeight);
            SetOptional(config.MaxWidth, ref maxWidth);
            SetOptional(config.MaxHeight, ref maxHeight);
            SetOptional(config.IdealWidth, ref idealWidth);
            SetOptional(config.IdealHeight, ref idealHeight);

            SetOptional(config.MinFrameRate, ref minFrameRate);
            SetOptional(config.MaxFrameRate, ref maxFrameRate);
            SetOptional(config.IdealFrameRate, ref idealFrameRate);


            CAPI.Unity_MediaNetwork_Configure(mReference,
                config.Audio, config.Video,
                minWidth, minHeight,
                maxWidth, maxHeight,
                idealWidth, idealHeight,
                minFrameRate, maxFrameRate, idealFrameRate, config.VideoDeviceName
                );
        }
        public IFrame TryGetFrame(ConnectionId id)
        {
            Texture2D buff = null;
            
            if (mFormat == FramePixelFormat.Native)
            {
                int[] width = new int[1];
                int[] height = new int[1];
                bool hasFrame = CAPI.Unity_MediaNetwork_TryGetFrame_Resolution(mReference, id.id, width, height);
                if (hasFrame == false)
                    return null;

                if (buff == null || buff.width != width[0] || buff.height != height[0])
                {
                    //must be in sync with RawFrame.ts
                    //RGB, mipmaps off
                    buff = new Texture2D(width[0], height[0], TextureFormat.RGB24, false);
                }
                int textureId = (int)buff.GetNativeTexturePtr();
                //

                bool res = CAPI.Unity_MediaNetwork_TryGetFrame_ToTexture(mReference, id.id, width[0], height[0], textureId);
                if (res == false)
                {
                    //this should never happen unless the browser is able to change the bufferd image between
                    //Unity_MediaNetwork_TryGetFrame_Resolution
                    //and the ToTexture call or there is a bug 
                    Debug.LogWarning("Skipped frame. Failed to move image into texture");
                    return null;
                }
                else
                {
                    return new TextureFrame(buff);
                }
            }
            else if (mFormat == FramePixelFormat.ABGR)
            {
                int length = CAPI.Unity_MediaNetwork_TryGetFrameDataLength(mReference, id.id);
                if (length < 0)
                    return null;

                int[] width = new int[1];
                int[] height = new int[1];
                byte[] buffer = new byte[length];

                bool res = CAPI.Unity_MediaNetwork_TryGetFrame(mReference, id.id, width, height, buffer, 0, buffer.Length);
                if (res)
                    return new BufferedFrame(buffer, width[0], height[0], FramePixelFormat.ABGR, 0, true);
                return null;
            }
            else
            {
                return null;
            }
        }

        public MediaConfigurationState GetConfigurationState()
        {
            int res = CAPI.Unity_MediaNetwork_GetConfigurationState(mReference);
            MediaConfigurationState state = (MediaConfigurationState)res;
            return state;
        }
        public override void Update()
        {
            base.Update();

        }
        public string GetConfigurationError()
        {
            if (GetConfigurationState() == MediaConfigurationState.Failed)
            {
                var err = CAPI.MediaNetwork_GetConfigurationError(mReference);
                return "" + err + " Check the browser log for more details.";
            }
            else
            {
                return null;
            }

        }

        public void ResetConfiguration()
        {
            CAPI.Unity_MediaNetwork_ResetConfiguration(mReference);
        }

        public void SetVolume(double volume, ConnectionId remoteUserId)
        {
            CAPI.Unity_MediaNetwork_SetVolume(mReference, volume, remoteUserId.id);
        }

        public bool HasAudioTrack(ConnectionId remoteUserId)
        {
            return CAPI.Unity_MediaNetwork_HasAudioTrack(mReference, remoteUserId.id);
        }

        public bool HasVideoTrack(ConnectionId remoteUserId)
        {
            return CAPI.Unity_MediaNetwork_HasVideoTrack(mReference, remoteUserId.id);
        }

        public bool IsMute()
        {
            return CAPI.Unity_MediaNetwork_IsMute(mReference);
        }

        public void SetMute(bool val)
        {
            CAPI.Unity_MediaNetwork_SetMute(mReference, val);
        }
    }
}
