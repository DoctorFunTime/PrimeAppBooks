using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace PrimeAppBooks.Interfaces
{
    public enum AnimationDirection
    {
        FromLeft,
        FromRight,
        FromTop,
        FromBottom,
        FadeIn,
        ZoomIn,
        SlideIn,
        Custom
    }

    public enum AnimationEasing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Bounce,
        Elastic,
        Back
    }

    public interface IAnimatedPage
    {
        /// <summary>
        /// The direction from which the page should animate in
        /// </summary>
        AnimationDirection AnimationDirection { get; }
        
        /// <summary>
        /// Custom animation style string for backward compatibility
        /// </summary>
        string AnimationStyle { get; }
        
        /// <summary>
        /// Duration of the animation in milliseconds
        /// </summary>
        int AnimationDuration { get; }
        
        /// <summary>
        /// Easing function to use for the animation
        /// </summary>
        AnimationEasing AnimationEasing { get; }
        
        /// <summary>
        /// Whether to animate the page out when navigating away
        /// </summary>
        bool AnimateOut { get; }
        
        /// <summary>
        /// Custom animation parameters (for advanced usage)
        /// </summary>
        object CustomAnimationParameters { get; }
    }
}