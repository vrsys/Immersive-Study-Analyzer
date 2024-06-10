// VRSYS plugin of Virtual Reality and Visualization Research Group (Bauhaus University Weimar)
//  _    ______  _______  _______
// | |  / / __ \/ ___/\ \/ / ___/
// | | / / /_/ /\__ \  \  /\__ \ 
// | |/ / _, _/___/ /  / /___/ / 
// |___/_/ |_|/____/  /_//____/  
//
//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2022 Virtual Reality and Visualization Research Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Ephraim Schott
//   Date:           2022
//-----------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Vrsys;
using Vrsys.Scripts.Recording;

public class TooltipHandler : MonoBehaviourPun
{
    public TooltipMapper leftTooltipMapper;
    public TooltipMapper rightTooltipMapper;

    public InputActionProperty toggleTooptipsAction;

    public bool hasTooltips = false;
    public Tooltip toggleTooltip;
    public bool playback = false;
    
    public bool showTooltips { get; private set; } = false;
    private bool _initialized = false;

    public Dictionary<string, Tooltip> tooltips = new Dictionary<string, Tooltip>();

    public List<Tooltip> baseTooltips = new List<Tooltip>();

    private GameObject helpUI = null;
    private int state = 0;
    private bool activated;
    
    // Start is called before the first frame update
    void Start()
    {
        if (!photonView.IsMine)
            enabled = false;
        else
            InitializeTooltips();
        
        //Tooltip radialMenu = new Tooltip();
        //radialMenu.hand = TooltipHand.Left;
        //radialMenu.tooltipName = "Menu";
        //radialMenu.tooltipText = "Menu";
        //radialMenu.actionButtonReference = Tooltip.ActionButton.PrimaryButton;
        //AddBaseTooltip(radialMenu);

        Tooltip teleport = new Tooltip();
        teleport.hand = TooltipHand.Left;
        teleport.tooltipName = "Teleport";
        teleport.tooltipText = "Teleport";
        teleport.actionButtonReference = Tooltip.ActionButton.Trigger;
        AddBaseTooltip(teleport);
    }

    // Update is called once per frame
    void Update()
    {
        if (toggleTooptipsAction.action.WasReleasedThisFrame())
            state = (state + 1) % 3;
        
        if(state == 0  && showTooltips){
            showTooltips = false;
            leftTooltipMapper.isActive = false;
            rightTooltipMapper.isActive = false;

            HideAllTooltips();
            HideBaseTooltips();
            //ShowTooltip(toggleTooltip);
            
            UpdateTooltipText(toggleTooltip, "Help");
            
            if(helpUI == null)
                helpUI = Utils.GetChildBySubstring(NetworkUser.localGameObject, "HelpOverview");

            if (helpUI != null)
                helpUI.SetActive(false);
            
            //ShowTooltip("Play/Pause");
            //if(playback)
            //    ShowTooltip("AnnotationType");
        } else if (state == 1)
        {
            UpdateTooltipText(toggleTooltip, "Help 1/2");
            
            if(helpUI == null)
                helpUI = Utils.GetChildBySubstring(NetworkUser.localGameObject, "HelpOverview");

            if (helpUI != null)
                helpUI.SetActive(true);
            
            //ShowTooltip("Play/Pause");
            //if(playback)
            //    ShowTooltip("AnnotationType");
        }
        else if (state == 2 && !showTooltips)
        {
            showTooltips = true;
            leftTooltipMapper.isActive = true;
            rightTooltipMapper.isActive = true;
            ShowBaseTooltips();

            if(helpUI == null)
                helpUI = Utils.GetChildBySubstring(NetworkUser.localGameObject, "HelpOverview");

            if (helpUI != null)
                helpUI.SetActive(false);
            
            if (playback)
            {
                //ShowTooltip("Play/Pause");
                ShowTooltip("Temporal navigation");
                ShowTooltip("TimePortal");
                ShowTooltip("TimeLine");
                ShowTooltip("Annotate");
                //ShowTooltip("AnnotationType");
                activated = true;
            }

            //ShowTooltip(toggleTooltip);
            UpdateTooltipText(toggleTooltip, "Help 2/2");
        }

        if (state == 2 && !activated)
        {
            if (playback)
            {
                //ShowTooltip("Play/Pause");
                ShowTooltip("Temporal navigation");
                ShowTooltip("TimePortal");
                ShowTooltip("TimeLine");
                ShowTooltip("Annotate");
                //ShowTooltip("AnnotationType");
                activated = true;
            }
        }
        
        ShowTooltip("Play/Pause");
        if(playback)
            ShowTooltip("AnnotationType");
    }

    void InitializeTooltips()
    {
        toggleTooptipsAction.action.Enable();
        if (hasTooltips)
        {
            this.ShowTooltip(toggleTooltip);
        }

        _initialized = true;
    }

    public void ShowAllTooltips()
    {
        if (!showTooltips)
            return;

        foreach (var tPair in tooltips)
        {
            if (tPair.Value.hand == TooltipHand.Left)
            {
                leftTooltipMapper.ShowTooltip(tPair.Value);
            }
            else if (tPair.Value.hand == TooltipHand.Right)
            {
                rightTooltipMapper.ShowTooltip(tPair.Value);
            }
            else if (tPair.Value.hand == TooltipHand.Both)
            {
                leftTooltipMapper.ShowTooltip(tPair.Value);
                rightTooltipMapper.ShowTooltip(tPair.Value);
            }
        }
    }

    public void HideAllTooltips()
    {
        foreach (var tPair in tooltips)
        {
            if (tPair.Value.hand == TooltipHand.Left)
            {
                leftTooltipMapper.HideTooltip(tPair.Value);
            }
            else if (tPair.Value.hand == TooltipHand.Right)
            {
                rightTooltipMapper.HideTooltip(tPair.Value);
            }
            else if (tPair.Value.hand == TooltipHand.Both)
            {
                leftTooltipMapper.HideTooltip(tPair.Value);
                rightTooltipMapper.HideTooltip(tPair.Value);
            }
        }
    }

    public void ShowTooltip(Tooltip tooltip, bool add = false)
    {
        if (add)
        {
            if (!tooltips.ContainsKey(tooltip.tooltipName))
            {
                tooltips.Add(tooltip.tooltipName, tooltip);
            }
            else
            {
                tooltips.Remove(tooltip.tooltipName);
                tooltips.Add(tooltip.tooltipName, tooltip);
            }
        }

        if (!showTooltips && _initialized)
            return;

        if (tooltip.hand == TooltipHand.Left)
        {
            leftTooltipMapper.ShowTooltip(tooltip);
        }
        else if (tooltip.hand == TooltipHand.Right)
        {
            rightTooltipMapper.ShowTooltip(tooltip);
        }
        else if (tooltip.hand == TooltipHand.Both)
        {
            leftTooltipMapper.ShowTooltip(tooltip);
            rightTooltipMapper.ShowTooltip(tooltip);
        }
    }

    public void ShowTooltip(string name)
    {
        if (tooltips.ContainsKey(name))
        {
            Tooltip tooltip = tooltips[name];
            if (tooltip.hand == TooltipHand.Left)
            {
                leftTooltipMapper.ShowTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Right)
            {
                rightTooltipMapper.ShowTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Both)
            {
                leftTooltipMapper.ShowTooltip(tooltip);
                rightTooltipMapper.ShowTooltip(tooltip);
            }
        }
    }

    public void UpdateTooltipText(Tooltip tooltip, string text)
    {
        if (tooltip.hand == TooltipHand.Left)
        {
            leftTooltipMapper.UpdatedTooltipText(tooltip, text);
        }
        else if (tooltip.hand == TooltipHand.Right)
        {
            rightTooltipMapper.UpdatedTooltipText(tooltip, text);
        }
        else if (tooltip.hand == TooltipHand.Both)
        {
            leftTooltipMapper.UpdatedTooltipText(tooltip, text);
            rightTooltipMapper.UpdatedTooltipText(tooltip, text);
        }
    }
    
    public void UpdateTooltipColor(Tooltip tooltip, Color clr)
    {
        if (tooltip.hand == TooltipHand.Left)
        {
            leftTooltipMapper.UpdatedTooltipColor(tooltip, clr);
        }
        else if (tooltip.hand == TooltipHand.Right)
        {
            rightTooltipMapper.UpdatedTooltipColor(tooltip, clr);
        }
        else if (tooltip.hand == TooltipHand.Both)
        {
            leftTooltipMapper.UpdatedTooltipColor(tooltip, clr);
            rightTooltipMapper.UpdatedTooltipColor(tooltip, clr);
        }
    }

    public void AddBaseTooltip(Tooltip tooltip)
    {
        baseTooltips.Add(tooltip);
    }

    public void ShowBaseTooltips()
    {
        foreach (var tooltip in baseTooltips)
        {
            if (tooltip.hand == TooltipHand.Left)
            {
                leftTooltipMapper.ShowTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Right)
            {
                rightTooltipMapper.ShowTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Both)
            {
                leftTooltipMapper.ShowTooltip(tooltip);
                rightTooltipMapper.ShowTooltip(tooltip);
            }
        }
    }

    public void HideBaseTooltips()
    {
        foreach (var tooltip in baseTooltips)
        {
            if (tooltip.hand == TooltipHand.Left)
            {
                leftTooltipMapper.HideTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Right)
            {
                rightTooltipMapper.HideTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Both)
            {
                leftTooltipMapper.HideTooltip(tooltip);
                rightTooltipMapper.HideTooltip(tooltip);
            }
        }
    }

    public void HideTooltip(Tooltip tooltip, bool remove = false)
    {
        if (remove)
        {
            if (tooltips.ContainsKey(tooltip.tooltipName))
            {
                tooltips.Remove(tooltip.tooltipName);
            }
        }

        if (tooltip.hand == TooltipHand.Left)
        {
            leftTooltipMapper.HideTooltip(tooltip);
        }
        else if (tooltip.hand == TooltipHand.Right)
        {
            rightTooltipMapper.HideTooltip(tooltip);
        }
        else if (tooltip.hand == TooltipHand.Both)
        {
            leftTooltipMapper.HideTooltip(tooltip);
            rightTooltipMapper.HideTooltip(tooltip);
        }
    }

    public void AddTooltip(Tooltip tooltip, bool show = true)
    {
        if (!tooltips.ContainsKey(tooltip.tooltipName))
        {
            tooltips.Add(tooltip.tooltipName, tooltip);
            
        }

        if (show && showTooltips)
        {
            if (tooltip.hand == TooltipHand.Left)
            {
                leftTooltipMapper.HideTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Right)
            {
                rightTooltipMapper.HideTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Both)
            {
                leftTooltipMapper.HideTooltip(tooltip);
                rightTooltipMapper.HideTooltip(tooltip);
            }
        }
    }

    public void RemoveTooltip(Tooltip tooltip)
    {
        if (tooltips.ContainsKey(tooltip.tooltipName))
        {
            tooltips.Remove(tooltip.tooltipName);

            if (tooltip.hand == TooltipHand.Left)
            {
                leftTooltipMapper.HideTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Right)
            {
                rightTooltipMapper.HideTooltip(tooltip);
            }
            else if (tooltip.hand == TooltipHand.Both)
            {
                leftTooltipMapper.HideTooltip(tooltip);
                rightTooltipMapper.HideTooltip(tooltip);
            }
        }
    }
}