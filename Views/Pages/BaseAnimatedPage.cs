using PrimeAppBooks.Interfaces;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Base class for pages that want to use enhanced animations
    /// </summary>
    public class BaseAnimatedPage : Page, IAnimatedPage
    {
        public BaseAnimatedPage()
        {
            // Set default animation properties - fix the enum access
            AnimationDirection = Interfaces.AnimationDirection.FromBottom;
            AnimationDuration = 400;
            AnimationEasing = Interfaces.AnimationEasing.EaseOut;
            AnimateOut = true;
        }

        public virtual Interfaces.AnimationDirection AnimationDirection { get; protected set; }
        public virtual string AnimationStyle => AnimationDirection.ToString();
        public virtual int AnimationDuration { get; protected set; }
        public virtual Interfaces.AnimationEasing AnimationEasing { get; protected set; }
        public virtual bool AnimateOut { get; protected set; }
        public virtual object CustomAnimationParameters => null;

        protected void SetAnimationDirection(Interfaces.AnimationDirection direction)
        {
            AnimationDirection = direction;
        }

        protected void SetAnimationDuration(int duration)
        {
            AnimationDuration = duration;
        }

        protected void SetAnimationEasing(Interfaces.AnimationEasing easing)
        {
            AnimationEasing = easing;
        }

        protected void SetAnimateOut(bool animateOut)
        {
            AnimateOut = animateOut;
        }
    }
}