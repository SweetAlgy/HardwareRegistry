import { bootstrapApplication } from '@angular/platform-browser';
import { loadMessages, locale } from 'devextreme/localization';
import ruMessages from 'devextreme/localization/messages/ru.json';
import { appConfig } from './app/app.config';
import { App } from './app/app';

loadMessages(ruMessages);
locale('ru');

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
