import { NgModule, ModuleWithProviders, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import {
  IonBackButton, IonModal, IonPopover, IonRouterOutlet, IonRouterLink, IonRouterLinkWithHref,
  IonTabs, IonNav, IonCheckbox, IonDatetime, IonInput, IonInputOtp, IonIcon,
  IonRadioGroup, IonRange, IonSearchbar, IonSegment, IonSelect, IonTextarea, IonToggle,
  IonAccordion, IonAccordionGroup, IonActionSheet, IonAlert, IonApp, IonAvatar,
  IonBackdrop, IonBadge, IonBreadcrumb, IonBreadcrumbs, IonButton, IonButtons,
  IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle,
  IonChip, IonCol, IonContent, IonDatetimeButton, IonFab, IonFabButton,
  IonFabList, IonFooter, IonGrid, IonHeader, IonImg, IonInfiniteScroll,
  IonInfiniteScrollContent, IonInputPasswordToggle, IonItem, IonItemDivider,
  IonItemGroup, IonItemOption, IonItemOptions, IonItemSliding, IonLabel,
  IonList, IonListHeader, IonLoading, IonMenu, IonMenuButton, IonMenuToggle,
  IonNavLink, IonNote, IonPicker, IonPickerColumn, IonPickerColumnOption,
  IonProgressBar, IonRadio, IonRefresher, IonRefresherContent, IonReorder,
  IonReorderGroup, IonRippleEffect, IonRow, IonSegmentButton, IonSegmentContent,
  IonSegmentView, IonSelectModal, IonSelectOption, IonSkeletonText, IonSpinner,
  IonSplitPane, IonTab, IonTabBar, IonTabButton, IonText, IonThumbnail,
  IonTitle, IonToast, IonToolbar
} from '@ionic/angular';

const IONIC_COMPONENTS = [
  IonBackButton, IonModal, IonPopover, IonRouterOutlet, IonRouterLink, IonRouterLinkWithHref,
  IonTabs, IonNav, IonCheckbox, IonDatetime, IonInput, IonInputOtp, IonIcon,
  IonRadioGroup, IonRange, IonSearchbar, IonSegment, IonSelect, IonTextarea, IonToggle,
  IonAccordion, IonAccordionGroup, IonActionSheet, IonAlert, IonApp, IonAvatar,
  IonBackdrop, IonBadge, IonBreadcrumb, IonBreadcrumbs, IonButton, IonButtons,
  IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle,
  IonChip, IonCol, IonContent, IonDatetimeButton, IonFab, IonFabButton,
  IonFabList, IonFooter, IonGrid, IonHeader, IonImg, IonInfiniteScroll,
  IonInfiniteScrollContent, IonInputPasswordToggle, IonItem, IonItemDivider,
  IonItemGroup, IonItemOption, IonItemOptions, IonItemSliding, IonLabel,
  IonList, IonListHeader, IonLoading, IonMenu, IonMenuButton, IonMenuToggle,
  IonNavLink, IonNote, IonPicker, IonPickerColumn, IonPickerColumnOption,
  IonProgressBar, IonRadio, IonRefresher, IonRefresherContent, IonReorder,
  IonReorderGroup, IonRippleEffect, IonRow, IonSegmentButton, IonSegmentContent,
  IonSegmentView, IonSelectModal, IonSelectOption, IonSkeletonText, IonSpinner,
  IonSplitPane, IonTab, IonTabBar, IonTabButton, IonText, IonThumbnail,
  IonTitle, IonToast, IonToolbar
];

@NgModule({
  imports: [...IONIC_COMPONENTS],
  exports: [...IONIC_COMPONENTS],
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class IonicModule {
  static forRoot(): ModuleWithProviders<IonicModule> {
    return {
      ngModule: IonicModule,
      providers: []
    };
  }
}
