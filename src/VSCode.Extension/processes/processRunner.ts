import { spawnSync, exec } from 'child_process';
import { ProcessArgumentBuilder } from './processArgumentBuilder';

export class ProcessRunner {
    public static runSync(command: string, ...args: string[]): string | undefined {
        const result = spawnSync(command, args);
        if (result.error) {
            console.error(result.error);
            return undefined;
        }
        return result.stdout.toString().trimEnd();
    }
    public static async runAsync(builder: ProcessArgumentBuilder): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            exec(builder.build(), (error, stdout, stderr) => {
                if (error) {
                    console.error(stderr);
                    reject(stderr);
                }

                resolve(stdout.toString());
            })
        });
    }
}
