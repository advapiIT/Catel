﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UICompositionService.cs" company="Catel development team">
//   Copyright (c) 2008 - 2015 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using Catel.Caching;
    using Catel.Logging;
    using Catel.MVVM;
    using Catel.MVVM.Views;
    using Catel.Windows.Controls;

    using Microsoft.Practices.Prism.Regions;
    using Threading;

    /// <summary>
    /// The user interface composition service.
    /// </summary>
    public sealed class UICompositionService : ViewModelServiceBase, IUICompositionService
    {
        #region Constants
        /// <summary>
        /// Reference equals invalid operation exception message.
        /// </summary>
        private const string ReferenceEqualsInvalidOperationExceptionMessage = "The reference of 'viewModel' and 'parentViewModel' are equals, so can't activate a view model into it self";

        /// <summary>
        ///  Activation required invalid operation error message.
        /// </summary>
        private const string ActivationRequiredInvalidOperationErrorMessage = "The 'viewModel' must be show at least one time in a region";

        /// <summary>
        /// The log.
        /// </summary>
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     The cache storage.
        /// </summary>
        private static readonly ICacheStorage<int, IViewInfo> ViewInfoCacheStorage = new CacheStorage<int, IViewInfo>();
        #endregion

        #region Fields
        /// <summary>
        /// The region manager.
        /// </summary>
        private readonly IRegionManager _regionManager;

        /// <summary>
        /// The view manager.
        /// </summary>
        private readonly IViewManager _viewManager;

        /// <summary>
        /// The view locator.
        /// </summary>
        private readonly IViewLocator _viewLocator;

        /// <summary>
        /// The dispatcher service.
        /// </summary>
        private readonly IDispatcherService _dispatcherService;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UICompositionService"/> class.
        /// </summary>
        /// <param name="regionManager">The region manager</param>
        /// <param name="viewManager">The view manager</param>
        /// <param name="viewLocator">The view locator</param>
        /// <param name="dispatcherService">The dispatcher service</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="regionManager"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewManager"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewLocator"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="dispatcherService"/> is <c>null</c>.</exception>
        public UICompositionService(IRegionManager regionManager, IViewManager viewManager, IViewLocator viewLocator, IDispatcherService dispatcherService)
        {
            Argument.IsNotNull(() => regionManager);
            Argument.IsNotNull(() => viewManager);
            Argument.IsNotNull(() => viewLocator);
            Argument.IsNotNull(() => dispatcherService);

            _regionManager = regionManager;
            _viewManager = viewManager;
            _viewLocator = viewLocator;
            _dispatcherService = dispatcherService;
        }
        #endregion

        #region IUICompositionService Members

        /// <summary>
        /// Activates a view into a specific <see cref="IRegion" /> via <see cref="IRegionManager" /> from a given view model.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="parentViewModel">The parent view model.</param>
        /// <param name="regionName">The region name.</param>
        /// <exception cref="InvalidOperationException">If <paramref name="regionName" /> is <c>null</c> and the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="regionName" /> is <c>null</c> and the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="regionName" /> is <c>null</c> and the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <paramref name="viewModel" /> and <paramref name="parentViewModel" /> are equals.</exception>
        /// <exception cref="NotSupportedException">If the implementation of IRegionManager is not registered in the IoC container</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="regionName" /> is <c>null</c> or whitespace.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        public void Activate(IViewModel viewModel, IViewModel parentViewModel, string regionName)
        {
            Argument.IsNotNull(() => viewModel);
            Argument.IsNotNull(() => parentViewModel);
            Argument.IsNotNullOrWhitespace(() => regionName);

            if (ReferenceEquals(viewModel, parentViewModel))
            {
                var exception = new InvalidOperationException(ReferenceEqualsInvalidOperationExceptionMessage);

                Log.Error(exception);

                throw exception;
            }

            var viewsOfParentViewModel = _viewManager.GetViewsOfViewModel(parentViewModel);
            var regionInfo = viewsOfParentViewModel.OfType<DependencyObject>().Select(dependencyObject => dependencyObject.GetRegionInfo(regionName)).FirstOrDefault(info => info != null);
            if (regionInfo != null)
            {
                Activate(viewModel, regionInfo);

                var parentRelationalViewModel = parentViewModel as IRelationalViewModel;
                if (parentRelationalViewModel != null)
                {
                    parentRelationalViewModel.RegisterChildViewModel(viewModel);
                }

                var relationalViewModel = viewModel as IRelationalViewModel;
                if (relationalViewModel != null)
                {
                    relationalViewModel.SetParentViewModel(parentViewModel);
                }
            }
        }

        /// <summary>
        /// Activates a view into a specific <see cref="IRegion" /> from a given view model and the region name.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="regionName">The region name.</param>
        /// <exception cref="InvalidOperationException">If <paramref name="regionName" /> is <c>null</c> and the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="regionName" /> is <c>null</c> and the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="regionName" /> is <c>null</c> and the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="NotSupportedException">If the implementation of IRegionManager is not registered in the IoC container</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        public void Activate(IViewModel viewModel, string regionName = null)
        {
            Argument.IsNotNull(() => viewModel);

            if (string.IsNullOrEmpty(regionName))
            {
                Reactivate(viewModel);
            }
            else
            {
                Activate(viewModel, regionName, this._regionManager);
            }
        }

        /// <summary>
        /// Deactivates the views that belongs to the <paramref name="viewModel" /> instance.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <exception cref="InvalidOperationException">If the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="viewModel"/> is <c>null</c>.</exception>
        public void Deactivate(IViewModel viewModel)
        {
            Argument.IsNotNull(() => viewModel);

            var viewInfo = ViewInfoCacheStorage[viewModel.UniqueIdentifier];
            if (viewInfo == null)
            {
                throw new InvalidOperationException(ActivationRequiredInvalidOperationErrorMessage);
            }

            var view = viewInfo.View;
            var region = viewInfo.Region;

            _dispatcherService.Invoke(() =>
                {
                    region.Deactivate(view);
                    if (viewModel.IsClosed)
                    {
                        ViewInfoCacheStorage.Remove(viewModel.UniqueIdentifier, () => region.Remove(view));
                    }
                }, true);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Activates a view into a specific <see cref="IRegion" /> from a given view model, the region name and the <see cref="RegionManager"/>.
        /// </summary>
        /// <param name="viewModel">The view model</param>
        /// <param name="regionName">The region name</param>
        /// <param name="regionManager">The region Manager</param>
        private void Activate(IViewModel viewModel, string regionName, IRegionManager regionManager)
        {
            if (regionManager != null && regionManager.Regions.ContainsRegionWithName(regionName))
            {
                var viewInfo = ViewInfoCacheStorage.GetFromCacheOrFetch(viewModel.UniqueIdentifier, () => new ViewInfo(ViewHelper.ConstructViewWithViewModel(this._viewLocator.ResolveView(viewModel.GetType()), viewModel), regionManager.Regions[regionName]));

                var region = viewInfo.Region;
                var view = viewInfo.View;

                _dispatcherService.Invoke(() =>
                    {
                        if (!region.ActiveViews.Contains(view))
                        {
                            if (!region.Views.Contains(view))
                            {
                                region.Add(view);
                                viewModel.ClosedAsync += ViewModelOnClosedAsync;
                            }

                            region.Activate(view);
                        }
                    }, true);
            }
        }


        /// <summary>
        /// Activates a view into a specific <see cref="IRegion" /> from a given view model and the <see cref="IRegionInfo"/>.
        /// </summary>
        /// <param name="viewModel">The view model</param>
        /// <param name="regionInfo">The region info</param>
        private void Activate(IViewModel viewModel, IRegionInfo regionInfo)
        {
            Activate(viewModel, regionInfo.RegionName, regionInfo.RegionManager);
        }

        /// <summary>
        /// Called when a view model is closed.
        /// </summary>
        /// <param name="sender">The view model</param>
        /// <param name="e">The event args</param>
        private Task ViewModelOnClosedAsync(object sender, ViewModelClosedEventArgs e)
        {
            var viewModel = (IViewModel)sender;
            viewModel.ClosedAsync -= ViewModelOnClosedAsync;
            Deactivate(viewModel);

            return TaskHelper.Completed;
        }

        /// <summary>
        /// Reactivates a view from its viewmodel reference.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="InvalidOperationException">If the <paramref name="viewModel" /> was no show at least one time in a <see cref="IRegion" />.</exception>
        private void Reactivate(IViewModel viewModel)
        {
            var viewInfo = ViewInfoCacheStorage[viewModel.UniqueIdentifier];
            if (viewInfo == null)
            {
                throw new InvalidOperationException(ActivationRequiredInvalidOperationErrorMessage);
            }

            var view = viewInfo.View;
            var region = viewInfo.Region;

            _dispatcherService.Invoke(() => region.Activate(view), true);
        }
        #endregion

        #region Nested type: ViewInfo
        /// <summary>
        /// The view region item.
        /// </summary>
        private class ViewInfo : Tuple<FrameworkElement, IRegion>, IViewInfo
        {
            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="ViewInfo" /> class.
            /// </summary>
            /// <param name="view">The view.</param>
            /// <param name="region">The region.</param>
            public ViewInfo(FrameworkElement view, IRegion region)
                : base(view, region)
            {
            }
            #endregion

            #region IViewInfo Members
            /// <summary>
            /// Gets View.
            /// </summary>
            FrameworkElement IViewInfo.View
            {
                get { return Item1; }
            }

            /// <summary>
            /// Gets Region.
            /// </summary>
            IRegion IViewInfo.Region
            {
                get { return Item2; }
            }
            #endregion
        }
        #endregion
    }
}