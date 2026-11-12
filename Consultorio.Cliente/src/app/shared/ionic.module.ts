import { NgModule, ModuleWithProviders, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { ThemeToggleComponent } from './theme-toggle/theme-toggle.component';
import { CpfPipe, DataBrPipe, IdadePipe, LinhasPipe, MoedaPipe, SinaisVitaisPipe, TamanhoArquivoPipe, TelefonePipe } from './pipes/formatacao.pipes';
import { AlertasClinicosComponent } from './alertas-clinicos/alertas-clinicos.component';
import { AlertasModalComponent } from './alertas-clinicos/alertas-modal.component';
import { BuscaCidComponent } from './busca-cid/busca-cid.component';
import { MascaraDirective } from './directives/mascara.directive';
import { SelecionarPacienteComponent } from './selecionar-paciente/selecionar-paciente.component';

const COMPARTILHADOS = [
  ThemeToggleComponent, CpfPipe, TelefonePipe, DataBrPipe, IdadePipe, MascaraDirective, SelecionarPacienteComponent,
  SinaisVitaisPipe, LinhasPipe, TamanhoArquivoPipe, MoedaPipe, AlertasClinicosComponent, AlertasModalComponent, BuscaCidComponent
];

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
  declarations: [...COMPARTILHADOS],
  imports: [CommonModule, ...IONIC_COMPONENTS],
  exports: [...IONIC_COMPONENTS, ...COMPARTILHADOS],
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
