import { ItemTemplateWizard } from './itemTemplateWizard';
import * as res from '../resources/constants';
import * as vscode from 'vscode';

export class WizardController {
    public static activate(context: vscode.ExtensionContext) {
        context.subscriptions.push(vscode.commands.registerCommand(res.commandIdCreateItemTemplate, 
            async (path: vscode.Uri) => await ItemTemplateWizard.createTemplateAsync(path))
        );
    }
}
