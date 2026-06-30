import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const snowflakePattern = /^\d{17,20}$/;

export function snowflakeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) {
      return null;
    }
    return snowflakePattern.test(value) ? null : { snowflake: true };
  };
}

export function requiredWhenEnabled(enabledControlName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const parent = control.parent;
    if (!parent) {
      return null;
    }

    const enabled = parent.get(enabledControlName)?.value;
    const value = (control.value ?? '').toString().trim();

    if (enabled && !value) {
      return { requiredWhenEnabled: true };
    }

    return null;
  };
}
