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
using UnityEngine;

public class TooltipMapper : MonoBehaviour
{
    public TooltipHand hand;
    public List<TooltipReference> tooltipReferences = new List<TooltipReference>();
    public bool isActive = true;

    private void Awake()
    {
        foreach (TooltipReference tr in tooltipReferences)
        {
            HideTooltip(tr);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowTooltip(Tooltip tooltip)
    {
        TooltipReference reference = null;
        foreach (var tr in tooltipReferences)
        {
            if (tooltip.actionButtonReference == tr.reference)
            {
                reference = tr;
                break;
            }
        }
        if (reference != null)
        {
            reference.SetTooltip(tooltip);
            reference.gameObject.SetActive(true);
        } else
        {
            Debug.LogError("Tooltip could not be set. The referenced action could not be found.");
        }
    }
    
    public void ShowTooltip(TooltipReference tooltipRef)
    {
        tooltipRef.gameObject.SetActive(true);
    }

    public void HideTooltip(Tooltip tooltip)
    {
        foreach (var tr in tooltipReferences)
        {
            if (tooltip.actionButtonReference == tr.reference)
            {
                tr.gameObject.SetActive(false);
                break;
            }
        }
    }
    
    public void UpdatedTooltipText(Tooltip tooltip, string text)
    {
        foreach (var tr in tooltipReferences)
        {
            if (tooltip.actionButtonReference == tr.reference)
            {
                tr.tooltipText.text = text;
                break;
            }
        }
    }

    public void UpdatedTooltipColor(Tooltip tooltip, Color clr)
    {
        foreach (var tr in tooltipReferences)
        {
            if (tooltip.actionButtonReference == tr.reference)
            {
                tr.tooltipText.color = clr;
                break;
            }
        }
    }

    public void HideTooltip(TooltipReference tooltipRef)
    {
        tooltipRef.gameObject.SetActive(false);
    }
}
