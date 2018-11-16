using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

/**<summary>
    * Helper class to easily animate objects with or without a delay. 
    * This class can also execute delayed actions.
    * Animations are stored as XAML "Storyboard" objects inside "Window.Resources" inside the base calling window specified in the constructor. The "x:Key" attribute is used to select the animation.
    * </summary> 
    * <example>
    * <Window.Resources>
    <Storyboard x:Key="Show0.25" Storyboard.TargetProperty="Opacity" FillBehavior="Stop">
        <DoubleAnimation From="0" To="0.25" Duration="0:0:1">
            <DoubleAnimation.EasingFunction>
                <CubicEase EasingMode="EaseInOut"/>
            </DoubleAnimation.EasingFunction>
        </DoubleAnimation>
    </Storyboard>
    </Window.Resources>
    * </example>
    */
public class AnimationHelper
{
    private Timer _timer = null; //Timer to check for due animations and actions in the pools.
    private Window _baseWindow = null; //Calling window to execute animations and actions on.
    private List<DelayAnimation> _delayAnimations = new List<DelayAnimation>(); //Pool to store animations being delayed.
    private List<DelayAction> _delayActions = new List<DelayAction>(); //Pool to store actions being delayed.

    /**<summary>
        * Dynamic helper to execute animations and actions with specific parameters.
        * </summary>
        * <param name="baseWindow">
        * Calling WPF window to execute the animations.
        * </param>
        */
    public AnimationHelper(Window baseWindow)
    {
        _baseWindow = baseWindow;
        _timer = new Timer(new TimerCallback(timer_tick), null, 0, 1); //Set the timer to 1ms interval with the timer_tick callback
    }

    /**<summary>
        * Timer tick callback executed when timer interval is passed.
        * </summary>
        * <param name="state">
        * Unused object to match callback parameters.
        * </param>
        */
    private void timer_tick(object state)
    {
        lock (_delayAnimations) //Threadsafe list access
        {
            List<DelayAnimation> toDelete = new List<DelayAnimation>(); //List of delayed animations to delete after completion.
            foreach (DelayAnimation delayAnimation in _delayAnimations) //Itterate over all delayed animations
            {
                if (DateTime.Now > delayAnimation.executeTime) //If animation is due for execution
                {
                    try
                    {
                        _baseWindow.Dispatcher.Invoke(() => Animate(delayAnimation), DispatcherPriority.Background); //Invoke the animation on the base window.
                    }
                    catch { }
                    toDelete.Add(delayAnimation); //Call for deletion of old animation.
                }
            }

            //Errors will occur if objects are deleted from a list while itterating over it:
            toDelete.ForEach(x => _delayAnimations.Remove(x)); //Delete old animations after itteration is finished
        }

        lock (_delayActions) //Threadsafe list access
        {
            List<DelayAction> toDelete = new List<DelayAction>(); //List of delayed actions to delete after completion.
            foreach (DelayAction delayAction in _delayActions) //Itterate over all delayed actions
            {
                if (DateTime.Now > delayAction.executeTime) //If action is due
                {
                    _baseWindow.Dispatcher.Invoke(delayAction.action, DispatcherPriority.Background); //Invoke the action on the base window.
                    toDelete.Add(delayAction); //Call for deletion of old action.
                }
            }

            //Errors will occur if objects are deleted from a list while itterating over it:
            toDelete.ForEach(x => _delayActions.Remove(x)); //Delete old actions after itteration is finished
        }
    }

    #region Animation Constructors

    /**<summary>
        * Instantly animate one or more elements.
        * </summary>
        * <param name="elements">
        * Array of elements to animate.
        * </param>
        * <param name="key">
        * Animation resource key.
        * </param>
        */
    public void Animate(string key, params FrameworkElement[] elements)
    {
        Animate(new Animation(key, elements));
    }

    /**<summary>
        * Instantly animate one or more elements.
        * </summary>
        * <param name="animation">
        * Animation object containing animation parameters.
        * </param>
        */
    public void Animate(Animation animation)
    {
        var story = (Storyboard)_baseWindow.FindResource(animation.key); //Find animation
        foreach (FrameworkElement element in animation.elements)
        {
            story?.Begin(element, true); //If storyboard animation exist, begin animation.
        }
    }

    #endregion

    #region Delayed Animation Constructors

    /**<summary>
        * Animate one or more elements with a delay.
        * </summary>
        * <param name="delay">
        * Time delay in milliseconds before the animation will execute.
        * </param>
        * <param name="elements">
        * Array of elements to animate.
        * </param>
        * <param name="key">
        * Animation resource key.
        * </param>
        */
    public void DelayAnimate(float delay, string key, params FrameworkElement[] elements)
    {
        DelayAnimate(TimeSpan.FromMilliseconds(delay), new Animation(key, elements));
    }

    /**<summary>
        * Animate one or more elements with a delay.
        * </summary>
        * <param name="delay">
        * Time delay in milliseconds before the animation will execute.
        * </param>
        * <param name="animation">
        * Animation object containing animation parameters.
        * </param>
        */
    public void DelayAnimate(float delay, Animation animation)
    {
        DelayAnimate(TimeSpan.FromMilliseconds(delay), animation);
    }

    /**<summary>
        * Animate one or more elements with a delay.
        * </summary>
        * <param name="delay">
        * Time delay before the animation will execute.
        * </param>
        * <param name="elements">
        * Array of elements to animate.
        * </param>
        * <param name="key">
        * Animation resource key.
        * </param>
        */
    public void DelayAnimate(TimeSpan delay, string key, params FrameworkElement[] elements)
    {
        DelayAnimate(delay, new Animation(key, elements));
    }

    /**<summary>
        * Animate one or more elements with a delay.
        * </summary>
        * <param name="delay">
        * Time delay before the animation will execute.
        * </param>
        * <param name="animation">
        * Animation object containing animation parameters.
        * </param>
        */
    public void DelayAnimate(TimeSpan delay, Animation animation)
    {
        lock (_delayAnimations) //Threadsafe list access
            _delayAnimations.Add(new DelayAnimation(DateTime.Now.Add(delay), animation)); //Add animation to pool
    }

    #endregion

    #region Delayed Action Constructors 

    /**<summary>
        * Execute action after specific time delay.
        * </summary>
        * <param name="delay">
        * Time delay in milliseconds before the action will execute.
        * </param>
        * <param name="action">
        * Action to execute after the delay.
        * </param>
        */
    public void DelayAct(float delay, Action action)
    {
        DelayAct(TimeSpan.FromMilliseconds(delay), action);
    }

    /**<summary>
        * Execute action after specific time delay.
        * </summary>
        * <param name="delay">
        * Time delay before the action will execute.
        * </param>
        * <param name="action">
        * Action to execute after the delay.
        * </param>
        */
    public void DelayAct(TimeSpan delay, Action action)
    {
        lock (_delayActions) //Threadsafe list access
            _delayActions.Add(new DelayAction(DateTime.Now.Add(delay), action)); //Add the action to the pool
    }

    #endregion

    #region General Utilities

    /**<summary>
        * Helper function to easily set opacity on multiple UI elements.
        * </summary>
        * <param name="elements">
        * Array of elements to change the opacity for.
        * </param>
        * <param name="Value">
        * New opacity value to update the elements with.
        * </param>
        */
    public void Opacity(double Value, params UIElement[] elements)
    {
        foreach (UIElement element in elements)
        {
            element.Opacity = Value;
        }
    }

    #endregion

    #region Classes

    /**<summary>
        * Animation parameter class. Stores information about an instant execution animation. 
        * </summary>
        */
    public class Animation
    {
        public FrameworkElement[] elements; //Stores elements to animate.
        public string key; //Animation resource key.

        /**<summary>
            * Animation constructor. Create new animation object.
            * </summary>
            * <param name="key">
            * Animation resource key.
            * </param>
            * <param name="elements">
            * Array of elements to animate.
            * </param>
            */
        public Animation(string key, params FrameworkElement[] elements)
        {
            this.key = key;
            this.elements = elements;
        }
    }

    /**<summary>
        * Delayed animation parameter class. Stores information about a delayed execution animation. 
        * </summary>
        */
    private class DelayAnimation : Animation
    {
        public DateTime executeTime; //Time when the animation will execute.

        /**<summary>
            * DelayedAnimation constructor. Create new delayed animation object.
            * </summary>
            * <param name="animation">
            * Animation to execute after delay.
            * </param>
            * <param name="executeTime">
            * Time when the animation will execute.
            * </param>
            */
        public DelayAnimation(DateTime executeTime, Animation animation) : base(animation.key, animation.elements)
        {
            this.executeTime = executeTime;
        }
    }

    /**<summary>
        * Delayed action parameter class. Stores information about a delayed execution action. 
        * </summary>
        */
    private class DelayAction
    {
        public DateTime executeTime; //Time when the action will execute
        public Action action; //Action to execute

        /**<summary>
            * DelayedAction constructor. Create new delayed action object.
            * </summary>
            * <param name="action">
            * Action to execute after delay.
            * </param>
            * <param name="executeTime">
            * Time when the action will execute.
            * </param>
            */
        public DelayAction(DateTime executeTime, Action action)
        {
            this.executeTime = executeTime;
            this.action = action;
        }
    }

    #endregion
}